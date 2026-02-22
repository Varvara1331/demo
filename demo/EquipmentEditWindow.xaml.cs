using demo.Data;
using demo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace demo
{
    public partial class EquipmentEditWindow : Window, INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private readonly Equipment _equipment;
        private readonly CurrentUser _user;
        private readonly bool _isNewEquipment;
        private string _originalPhotoPath;
        private bool _hasChanges = false;

        private string _windowTitle;
        private string _equipmentName;
        private string _inventoryNumber;
        private string _description;
        private int? _officeId;
        private int? _roomId;
        private decimal _weight;
        private int _serviceLifeYears;
        private DateTime _registrationDate;
        private string _photoPath;
        private bool _hasPhoto;
        private List<Office> _offices;
        private List<Room> _rooms;

        public event PropertyChangedEventHandler PropertyChanged;

        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; OnPropertyChanged(); }
        }

        public string EquipmentName
        {
            get => _equipmentName;
            set { _equipmentName = value; OnPropertyChanged(); }
        }

        public string InventoryNumber
        {
            get => _inventoryNumber;
            set { _inventoryNumber = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public int? OfficeId
        {
            get => _officeId;
            set
            {
                _officeId = value;
                OnPropertyChanged();
                RoomId = 0;
                LoadRooms();
            }
        }

        public int? RoomId
        {
            get => _roomId;
            set
            {
                _roomId = value;
                OnPropertyChanged();
            }
        }

        public decimal Weight
        {
            get => _weight;
            set { _weight = value; OnPropertyChanged(); }
        }

        public int ServiceLifeYears
        {
            get => _serviceLifeYears;
            set { _serviceLifeYears = value; OnPropertyChanged(); }
        }

        public DateTime RegistrationDate
        {
            get => _registrationDate;
            set { _registrationDate = value; OnPropertyChanged(); }
        }

        public string PhotoPath
        {
            get => _photoPath;
            set
            {
                _photoPath = value;
                OnPropertyChanged();
                HasPhoto = !string.IsNullOrEmpty(value) && value != GetStubPath();
            }
        }

        public bool HasPhoto
        {
            get => _hasPhoto;
            set { _hasPhoto = value; OnPropertyChanged(); }
        }

        public List<Office> Offices
        {
            get => _offices;
            set { _offices = value; OnPropertyChanged(); }
        }

        public List<Room> Rooms
        {
            get => _rooms;
            set { _rooms = value; OnPropertyChanged(); }
        }

        public bool CanEdit { get; private set; }
        public bool IsReadOnly => !CanEdit;
        public bool CanSelectOffice { get; private set; }
        public bool CanDelete { get; private set; }
        public Brush FieldBackground => CanEdit ? Brushes.White : Brushes.LightGray;

        public bool IsDateReadOnly => true;

        public EquipmentEditWindow(Equipment equipment, AppDbContext context, CurrentUser user)
        {
            InitializeComponent();
            _context = context;
            _user = user;
            _isNewEquipment = equipment == null;
            _equipment = equipment ?? new Equipment();

            DataContext = this;
            DeterminePermissions();
            LoadData();
            LoadOffices();
            LoadRooms();
        }

        private void DeterminePermissions()
        {
            bool isAdmin = _user.PositionName == "администратор бд" ||
                          _user.PositionName == "администратор";

            bool isManager = _user.PositionName == "заведующий лабораторией" ||
                            _user.PositionName == "заведующий складом";

            bool isTechnician = _user.PositionName == "техник";
            bool isEngineer = _user.PositionName == "инженер";

            CanEdit = (isAdmin || isManager) && !_isNewEquipment;

            if (_isNewEquipment)
            {
                CanEdit = isAdmin || isManager;
            }

            CanSelectOffice = isAdmin;

            if (_isNewEquipment)
            {
                CanDelete = false;
            }
            else
            {
                bool isStorage = _equipment.Office != null &&
                                _equipment.Office.ShortName.ToLower().Contains("склад");
                DateTime endDate = _equipment.RegistrationDate.AddYears(_equipment.ServiceLifeYears);
                bool isExpired = endDate < DateTime.Now;

                CanDelete = isAdmin && (isStorage || isExpired);
            }

            if (_isNewEquipment)
            {
                WindowTitle = "Добавление оборудования";
            }
            else
            {
                if (CanEdit)
                    WindowTitle = "Редактирование оборудования";
                else
                    WindowTitle = "Просмотр оборудования";
            }
        }

        private void LoadData()
        {
            if (_isNewEquipment)
            {
                EquipmentName = "";
                Description = "";
                InventoryNumber = GenerateInventoryNumber();

                if (!CanSelectOffice && _user.OfficeId > 0)
                {
                    OfficeId = _user.OfficeId;
                }
                else
                {
                    OfficeId = null;
                }

                RoomId = 0;
                Weight = 0;
                ServiceLifeYears = 1;
                RegistrationDate = DateTime.Now;
                PhotoPath = GetStubPath();
            }
            else
            {
                EquipmentName = _equipment.Name;
                InventoryNumber = _equipment.InventoryNumber;
                Description = _equipment.Description;
                OfficeId = _equipment.OfficeId ?? _equipment.Room?.OfficeId;
                RoomId = _equipment.RoomId ?? 0;
                Weight = _equipment.Weight;
                ServiceLifeYears = _equipment.ServiceLifeYears;
                RegistrationDate = _equipment.RegistrationDate;

                string? photoFileName = _equipment.PhotoPath;
                if (!string.IsNullOrEmpty(photoFileName))
                {
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", photoFileName);
                    PhotoPath = File.Exists(fullPath) ? fullPath : GetStubPath();
                    _originalPhotoPath = photoFileName;
                }
                else
                {
                    PhotoPath = GetStubPath();
                }
            }
        }

        private string GenerateInventoryNumber()
        {
            string prefix = "INV-";
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string randomPart = new Random().Next(1000, 9999).ToString();

            string number = $"{prefix}{datePart}-{randomPart}";

            while (_context.Equipment.Any(e => e.InventoryNumber == number))
            {
                randomPart = new Random().Next(1000, 9999).ToString();
                number = $"{prefix}{datePart}-{randomPart}";
            }

            return number;
        }

        private void LoadOffices()
        {
            var query = _context.Offices.AsQueryable();

            if (!CanSelectOffice)
            {
                query = query.Where(o => o.OfficeId == _user.OfficeId);
            }
            else if (!CanEdit)
            {
                query = query.Where(o => !o.IsSecret || _user.PositionName == "администратор бд");
            }

            Offices = query.OrderBy(o => o.FullName).ToList();
        }

        private void LoadRooms()
        {
            var roomsList = new List<Room>();

            roomsList.Add(new Room { RoomId = 0, RoomNumber = "Не выбрано" });

            if (OfficeId.HasValue && OfficeId.Value > 0)
            {
                var query = _context.Rooms
                    .Where(r => r.OfficeId == OfficeId.Value);

                if (!CanEdit)
                {
                    query = query.Where(r => !r.IsSecret || _user.PositionName == "администратор бд");
                }

                roomsList.AddRange(query.OrderBy(r => r.RoomNumber).ToList());
            }

            Rooms = roomsList;

            if (!RoomId.HasValue || RoomId.Value == 0)
            {
                RoomId = 0;
            }

            OnPropertyChanged(nameof(Rooms));
        }

        private string GetStubPath()
        {
            string stubPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "stub.jpg");
            return File.Exists(stubPath) ? stubPath : "";
        }

        private void SelectPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit) return;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
            dialog.Title = "Выберите фотографию оборудования";

            if (dialog.ShowDialog() == true)
            {
                string selectedFilePath = dialog.FileName;

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(selectedFilePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    if (bitmap.PixelWidth > 300 || bitmap.PixelHeight > 200)
                    {
                        MessageBox.Show("Максимальный размер изображения: 300x200", "Предупреждение",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении изображения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string imagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                string extension = Path.GetExtension(selectedFilePath);
                string newFileName = $"equip_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
                string destPath = Path.Combine(imagesFolder, newFileName);

                try
                {
                    File.Copy(selectedFilePath, destPath, true);

                    if (!string.IsNullOrEmpty(_originalPhotoPath) && _originalPhotoPath != "stub.jpg")
                    {
                        string oldPhotoPath = Path.Combine(imagesFolder, _originalPhotoPath);
                        if (File.Exists(oldPhotoPath))
                        {
                            try { File.Delete(oldPhotoPath); } catch { }
                        }
                    }

                    _originalPhotoPath = newFileName;
                    PhotoPath = destPath;
                    _equipment.PhotoPath = newFileName;
                    _hasChanges = true;
                    CommandManager.InvalidateRequerySuggested();
                    MessageBox.Show("Фотография успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении фотографии: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void DeleteOldPhoto()
        {
            if (string.IsNullOrEmpty(_originalPhotoPath) || _originalPhotoPath == "stub.jpg")
                return;

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", _originalPhotoPath);

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении: {ex.Message}");
            }
        }

        private void DeletePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit || !HasPhoto) return;

            var result = MessageBox.Show("Удалить фотографию?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string fileToDelete = _originalPhotoPath;
                    if (!string.IsNullOrEmpty(fileToDelete) && fileToDelete != "stub.jpg")
                    {
                        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", fileToDelete);

                        int maxAttempts = 3;
                        for (int i = 0; i < maxAttempts; i++)
                        {
                            try
                            {
                                if (File.Exists(fullPath))
                                {
                                    File.Delete(fullPath);
                                    break;
                                }
                            }
                            catch
                            {
                                if (i == maxAttempts - 1) throw;
                                System.Threading.Thread.Sleep(100);
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                        }
                    }

                    _originalPhotoPath = null;
                    _equipment.PhotoPath = null;
                    PhotoPath = GetStubPath();
                    _hasChanges = true;

                    CommandManager.InvalidateRequerySuggested();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEdit) return;

            if (string.IsNullOrWhiteSpace(EquipmentName))
            {
                MessageBox.Show("Введите наименование оборудования.");
                return;
            }

            if (Weight < 0)
            {
                MessageBox.Show("Вес не может быть отрицательным.");
                return;
            }

            if (ServiceLifeYears <= 0)
            {
                MessageBox.Show("Нормативный срок службы не может быть отрицательным.");
                return;
            }

            if (!OfficeId.HasValue)
            {
                MessageBox.Show("Выберите подразделение.");
                return;
            }

            try
            {
                _equipment.Name = EquipmentName;
                _equipment.Description = Description;
                _equipment.Weight = Weight;
                _equipment.ServiceLifeYears = ServiceLifeYears;

                _equipment.RoomId = (RoomId.HasValue && RoomId.Value > 0) ? RoomId.Value : (int?)null;

                if (_equipment.RoomId.HasValue)
                {
                    _equipment.OfficeId = null;
                }
                else
                {
                    _equipment.OfficeId = OfficeId;
                }

                if (_isNewEquipment)
                {
                    _equipment.InventoryNumber = InventoryNumber;
                    _equipment.RegistrationDate = RegistrationDate;
                    _equipment.PhotoPath = _originalPhotoPath;
                    _equipment.IsArchived = false;
                    _equipment.IsSecret = false;

                    _context.Equipment.Add(_equipment);
                }
                else
                {
                    _equipment.PhotoPath = _originalPhotoPath;
                }
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(_equipment);

                if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(_equipment, validationContext, validationResults, true))
                {
                    string errors = string.Join("\n", validationResults.Select(r => r.ErrorMessage));
                    MessageBox.Show($"Ошибка валидации данных:\n{errors}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _context.SaveChanges();

                MessageBox.Show("Данные успешно сохранены.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _hasChanges = false;
                this.Close();
            }
            catch (DbUpdateException dbEx)
            {
                string errorMessage = "Ошибка при сохранении в базу данных:\n";

                if (dbEx.InnerException != null)
                {
                    errorMessage += dbEx.InnerException.Message;

                    if (dbEx.InnerException.Message.Contains("FK__Equipment__Offic"))
                    {
                        errorMessage = "Ошибка внешнего ключа: Указанное подразделение не существует.";
                    }
                    else if (dbEx.InnerException.Message.Contains("FK__Equipment__RoomI"))
                    {
                        errorMessage = "Ошибка внешнего ключа: Указанная аудитория не существует.";
                    }
                    else if (dbEx.InnerException.Message.Contains("UQ__Equipmen__D6D65CC85ECD69FF"))
                    {
                        errorMessage = "Оборудование с таким инвентарным номером уже существует.";
                    }
                    else if (dbEx.InnerException.Message.Contains("Cannot insert the value NULL"))
                    {
                        errorMessage = "Не все обязательные поля заполнены.";
                    }
                }
                else
                {
                    errorMessage += dbEx.Message;
                }

                MessageBox.Show(errorMessage, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n\nStack: {ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CanDelete || _isNewEquipment) return;

            var result = MessageBox.Show(
                "Вы действительно хотите удалить это оборудование?\n" +
                "Оборудование можно удалить только со склада или с истекшим сроком службы.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DeleteOldPhoto();

                    _context.Equipment.Remove(_equipment);
                    _context.SaveChanges();

                    MessageBox.Show("Оборудование удалено.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    _hasChanges = false;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show("Есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            if (_hasChanges && !string.IsNullOrEmpty(_originalPhotoPath))
            {
                DeleteOldPhoto();
            }

            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}