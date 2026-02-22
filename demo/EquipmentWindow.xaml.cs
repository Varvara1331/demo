using demo.Data;
using demo.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace demo
{
    public partial class EquipmentWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private bool _canSeeStatus;
        private bool _canUseFilters;
        private bool _canAddEdit;
        private bool _canViewDetails;
        private static bool _isEditWindowOpen = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanSeeStatus
        {
            get => _canSeeStatus;
            set
            {
                _canSeeStatus = value;
                OnPropertyChanged();
            }
        }

        public bool CanUseFilters
        {
            get => _canUseFilters;
            set
            {
                _canUseFilters = value;
                OnPropertyChanged();
            }
        }

        public bool CanAddEdit
        {
            get => _canAddEdit;
            set
            {
                _canAddEdit = value;
                OnPropertyChanged();
            }
        }

        public bool CanViewDetails
        {
            get => _canViewDetails;
            set
            {
                _canViewDetails = value;
                OnPropertyChanged();
            }
        }

        private class FilterSortOptions
        {
            public string SearchText { get; set; } = "";
            public int? SelectedOfficeId { get; set; }
            public string SortOrder { get; set; } = "None";
        }

        private FilterSortOptions _currentOptions = new FilterSortOptions();
        private List<OfficeFilterItem> _officeFilters;

        public EquipmentWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            DataContext = this;
            LoadUserInfo();
            LoadOffices();
            LoadEquipment();
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void LoadUserInfo()
        {
            txtUserInfo.Text = $"{MainWindow.User.FullName}";
            Title = $"Список оборудования - {MainWindow.User.FullName}";

            CanSeeStatus = MainWindow.User.PositionName == "заведующий лабораторией" ||
                           MainWindow.User.PositionName == "администратор бд";

            CanUseFilters = MainWindow.User.PositionName == "инженер" ||
                           MainWindow.User.PositionName == "администратор бд" ||
                           MainWindow.User.PositionName == "администратор";

            CanAddEdit = MainWindow.User.PositionName == "администратор бд" ||
                        MainWindow.User.PositionName == "администратор" ||
                        MainWindow.User.PositionName == "заведующий лабораторией" ||
                        MainWindow.User.PositionName == "заведующий складом";

            CanViewDetails = MainWindow.User.PositionName == "техник" ||
                            MainWindow.User.PositionName == "инженер" ||
                            CanAddEdit;
        }

        private void LoadOffices()
        {
            try
            {
                var offices = _context.Offices
                    .Where(o => !o.IsSecret || MainWindow.User.PositionName == "администратор бд")
                    .OrderBy(o => o.FullName)
                    .Select(o => new OfficeFilterItem
                    {
                        OfficeId = o.OfficeId,
                        DisplayName = o.FullName
                    })
                    .ToList();

                _officeFilters = new List<OfficeFilterItem>
                {
                    new OfficeFilterItem { OfficeId = null, DisplayName = "Все подразделения" }
                };

                cmbOfficeFilter.ItemsSource = _officeFilters;
                cmbOfficeFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        public class OfficeFilterItem
        {
            public int? OfficeId { get; set; }
            public string? DisplayName { get; set; }
        }

        public void LoadEquipment()
        {
            try
            {
                DateTime currentDate = DateTime.Now;

                var query = _context.Equipment
                    .Include(e => e.Room)
                        .ThenInclude(r => r.Office)
                    .Include(e => e.Office)
                    .Where(e => !e.IsArchived)
                    .AsQueryable();

                query = ApplyUserAccessFilter(query);

                if (CanUseFilters && _currentOptions.SelectedOfficeId.HasValue)
                {
                    query = query.Where(e =>
                        (e.Office != null && e.OfficeId == _currentOptions.SelectedOfficeId) ||
                        (e.Room != null && e.Room.OfficeId == _currentOptions.SelectedOfficeId));
                }

                var equipmentList = query.ToList();

                if (CanUseFilters && !string.IsNullOrWhiteSpace(_currentOptions.SearchText))
                {
                    equipmentList = ApplyMultiWordSearch(equipmentList);
                }

                if (CanUseFilters)
                {
                    if (_currentOptions.SortOrder == "Ascending")
                    {
                        equipmentList = equipmentList.OrderBy(e => e.Weight).ToList();
                    }
                    else if (_currentOptions.SortOrder == "Descending")
                    {
                        equipmentList = equipmentList.OrderByDescending(e => e.Weight).ToList();
                    }
                }

                var viewModelList = equipmentList.Select(e => CreateEquipmentViewModel(e, currentDate)).ToList();
                lvEquipment.ItemsSource = viewModelList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки оборудования: {ex.Message}");
            }
        }

        private List<Equipment> ApplyMultiWordSearch(List<Equipment> equipmentList)
        {
            string[] searchWords = _currentOptions.SearchText
                .ToLower()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .ToArray();

            if (searchWords.Length == 0)
                return equipmentList;

            return equipmentList.Where(e =>
            {
                string searchableText = string.Join(" ",
                    e.Name ?? "",
                    e.Description ?? "",
                    e.InventoryNumber ?? "",
                    e.Office?.FullName ?? "",
                    e.Room?.Office?.FullName ?? "",
                    e.Room?.RoomNumber ?? ""
                ).ToLower();

                return searchWords.All(word => searchableText.Contains(word));
            }).ToList();
        }

        private IQueryable<Equipment> ApplyUserAccessFilter(IQueryable<Equipment> query)
        {
            if (MainWindow.User.IsGuest)
            {
                return query.Where(e =>
                    (e.Office != null && e.Office.ShortName.ToLower().Trim() == "столовая") ||
                    (e.Room != null && e.Room.Office.ShortName.ToLower().Trim() == "столовая")
                );
            }

            if (MainWindow.User.PositionName != "администратор бд" &&
                MainWindow.User.PositionName != "администратор" &&
                MainWindow.User.PositionName != "инженер")
            {
                return query.Where(e =>
                    (e.Room != null && e.Room.OfficeId == MainWindow.User.OfficeId) ||
                    (e.Office != null && e.OfficeId == MainWindow.User.OfficeId));
            }

            return query;
        }

        private EquipmentViewModel CreateEquipmentViewModel(Equipment e, DateTime currentDate)
        {
            DateTime endDate = e.RegistrationDate.AddYears(e.ServiceLifeYears);

            string statusText = "";
            Brush statusBrush = Brushes.Transparent;

            bool isStorage = e.Office != null && e.Office.ShortName.ToLower().Contains("склад");

            if (CanSeeStatus)
            {
                if (endDate < currentDate && !isStorage)
                {
                    statusText = "На списание";
                    statusBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E32636"));
                }
                else if (endDate.Year == currentDate.Year)
                {
                    statusText = "Срок службы истекает в этом году";
                    statusBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"));
                }
                else
                {
                    statusText = $"Срок службы до: {endDate:yyyy}";
                }
            }

            string office = e.Office != null ? e.Office.FullName :
                                   e.Room != null ? e.Room.Office.FullName : "";

            string room = e.Room != null ? e.Room.RoomNumber : "";

            return new EquipmentViewModel
            {
                EquipmentId = e.EquipmentId,
                Name = e.Name,
                Description = e.Description,
                PhotoImage = GetPhotoImage(e),
                Room = room,
                Office = office,
                StatusText = statusText,
                StatusColor = statusBrush,
                RegistrationDate = e.RegistrationDate,
                ServiceLifeYears = e.ServiceLifeYears,
                InventoryNumber = e.InventoryNumber,
                Weight = e.Weight,
                IsStorage = isStorage,
                IsExpired = endDate < currentDate,
                HasRoom = !string.IsNullOrEmpty(room),
                HasOffice = !string.IsNullOrEmpty(office)
            };
        }

        private BitmapImage GetPhotoImage(Equipment e)
        {
            string imagePath = "";

            if (!string.IsNullOrEmpty(e.PhotoPath))
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", e.PhotoPath);
                if (File.Exists(fullPath))
                {
                    imagePath = fullPath;
                }
            }

            if (string.IsNullOrEmpty(imagePath))
            {
                imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "stub.jpg");
                if (!File.Exists(imagePath)) return null;
            }

            try
            {
                var bitmap = new BitmapImage();

                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void txtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (CanUseFilters)
            {
                _currentOptions.SearchText = txtSearch.Text;
                LoadEquipment();
            }
        }

        private void cmbOfficeFilter_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CanUseFilters && cmbOfficeFilter.SelectedItem is OfficeFilterItem selectedItem)
            {
                _currentOptions.SelectedOfficeId = selectedItem.OfficeId;
                LoadEquipment();
            }
        }

        private void cmbSortOrder_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CanUseFilters && cmbSortOrder.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                _currentOptions.SortOrder = selectedItem.Tag as string ?? "None";
                LoadEquipment();
            }
        }

        private void AddEquipmentButton(object sender, RoutedEventArgs e)
        {
            if (!CanAddEdit) return;

            if (_isEditWindowOpen)
            {
                return;
            }

            var editWindow = new EquipmentEditWindow(null, _context, MainWindow.User);
            editWindow.Closed += (s, args) =>
            {
                _isEditWindowOpen = false;
                LoadEquipment();
            };
            _isEditWindowOpen = true;
            editWindow.ShowDialog();
        }

        private void lvEquipment_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvEquipment.SelectedItem is EquipmentViewModel selectedEquipment)
            {
                if (!CanViewDetails)
                {
                    return;
                }

                if (_isEditWindowOpen)
                {
                    return;
                }

                var equipment = _context.Equipment
                    .Include(e => e.Room)
                    .Include(e => e.Office)
                    .FirstOrDefault(e => e.EquipmentId == selectedEquipment.EquipmentId);

                if (equipment != null)
                {
                    var editWindow = new EquipmentEditWindow(equipment, _context, MainWindow.User);
                    editWindow.Closed += (s, args) =>
                    {
                        _isEditWindowOpen = false;
                        LoadEquipment();
                    };
                    _isEditWindowOpen = true;
                    editWindow.ShowDialog();
                }
            }
        }

        private void ExitButton(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _context?.Dispose();
            if (Application.Current.Windows.Count == 0)
                Application.Current.Shutdown();
        }
    }

    public class EquipmentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _room;
        private string? _office;

        public int EquipmentId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        private BitmapImage _photoImage;
        public BitmapImage PhotoImage
        {
            get => _photoImage;
            set
            {
                _photoImage = value;
                OnPropertyChanged();
            }
        }

        public string? Room
        {
            get => _room;
            set
            {
                _room = value;
                OnPropertyChanged();
                HasRoom = !string.IsNullOrEmpty(value);
            }
        }

        public string? Office
        {
            get => _office;
            set
            {
                _office = value;
                OnPropertyChanged();
                HasOffice = !string.IsNullOrEmpty(value);
            }
        }

        public string? StatusText { get; set; }
        public Brush? StatusColor { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int ServiceLifeYears { get; set; }
        public string? InventoryNumber { get; set; }
        public decimal Weight { get; set; }
        public bool IsStorage { get; set; }
        public bool IsExpired { get; set; }

        public bool HasRoom { get; set; }
        public bool HasOffice { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}