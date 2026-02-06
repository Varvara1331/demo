using demo.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace demo
{
    /// <summary>
    /// Логика взаимодействия для EquipmentWindow.xaml
    /// </summary>
    public partial class EquipmentWindow : Window
    {
        private readonly AppDbContext _context;
        public bool CanSeeStatus { get; set; }

        public EquipmentWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadUserInfo();
            LoadEquipment();
        }

        private void LoadUserInfo()
        {
            txtUserInfo.Text = $"Пользователь: {MainWindow.User.FullName}";
            Title = $"Список оборудования - {MainWindow.User.FullName}";
        }

        private void LoadEquipment()
        {
            try
            {
                DateTime currentDate = DateTime.Now;

                var query = _context.Equipment
                    .Include(e => e.Room)
                        .ThenInclude(r => r.Office)
                    .Include(e => e.Office)
                    .Where(e => !e.IsSecret && !e.IsArchived)
                    .AsEnumerable();

                CanSeeStatus = MainWindow.User.PositionName == "заведующий лабораторией" ||
                               MainWindow.User.PositionName == "администратор бд";

                if (MainWindow.User.IsGuest)
                {
                    query = query.Where(e =>
                        (e.Office != null && e.Office.ShortName.ToLower().Trim() == "столовая") ||
                        (e.Room != null && e.Room.Office.ShortName.ToLower().Trim() == "столовая")
                    );
                }
                else if (MainWindow.User.PositionName == "лаборант" ||
                         MainWindow.User.PositionName == "техник")
                {
                    query = query.Where(e =>
                        (e.Room != null && e.Room.OfficeId == MainWindow.User.OfficeId) ||
                        (e.Office != null && e.OfficeId == MainWindow.User.OfficeId));
                }
                else if (MainWindow.User.PositionName == "заведующий лабораторией" || MainWindow.User.PositionName == "заведующий складом")
                {
                    query = query.Where(e =>
                        (e.Room != null && e.Room.OfficeId == MainWindow.User.OfficeId) ||
                        (e.Office != null && e.OfficeId == MainWindow.User.OfficeId));
                }

                var equipmentList = query.Select(e =>
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

                    string photoPath = "";
                    if (!string.IsNullOrEmpty(e.PhotoPath))
                    {
                        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), e.PhotoPath);
                        if (File.Exists(fullPath))
                        {
                            photoPath = fullPath;
                        }
                    }

                    if (string.IsNullOrEmpty(photoPath))
                    {
                        string stubPath = Path.Combine(Directory.GetCurrentDirectory(), "stub.jpg");
                        photoPath = File.Exists(stubPath) ? stubPath : "";
                    }

                    return new EquipmentViewModel
                    {
                        EquipmentId = e.EquipmentId,
                        Name = e.Name,
                        Description = e.Description,
                        Photo = string.IsNullOrEmpty(e.PhotoPath)
                            ? "Images/stub.jpg"
                            : $"Images/{e.PhotoPath.Trim()}",
                        Room = room,
                        Office = office,
                        StatusText = statusText,
                        StatusColor = statusBrush,
                        RegistrationDate = e.RegistrationDate,
                        ServiceLifeYears = e.ServiceLifeYears
                    };
                }).ToList();

                lvEquipment.ItemsSource = equipmentList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки оборудования: {ex.Message}");
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
            if (Application.Current.Windows.Count == 0)
                Application.Current.Shutdown();
        }
    }

    public class EquipmentViewModel
    {
        public int EquipmentId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Photo { get; set; }
        public string? Room { get; set; }
        public string? Office { get; set; }
        public string? StatusText { get; set; }
        public Brush? StatusColor { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int ServiceLifeYears { get; set; }
    }
}
