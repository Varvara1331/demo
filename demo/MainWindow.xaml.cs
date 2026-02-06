using demo.Data;
using demo.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace demo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static CurrentUser User = new CurrentUser();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var login = txtLogin.Text.Trim();
                    var password = txtPassword.Password;

                    var worker = context.Workers
                        .Include(w => w.Position)
                        .Include(w => w.Office)
                        .FirstOrDefault(w => w.Login == login && w.Password == password);

                    if (worker != null)
                    {
                        User.WorkerId = worker.WorkerId;
                        User.FullName = $"{worker.LastName} {worker.FirstName} {worker.MiddleName}";
                        User.OfficeId = worker.OfficeId;
                        User.PositionName = worker.Position.PositionName;
                        User.IsGuest = false;

                        OpenEquipmentWindow();
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void GuestButton(object sender, RoutedEventArgs e)
        {
            User = new CurrentUser
            {
                FullName = "Гость",
                IsGuest = true
            };

            OpenEquipmentWindow();
        }

        private void OpenEquipmentWindow()
        {
            var equipmentWindow = new EquipmentWindow();
            equipmentWindow.Show();
            this.Close();
        }
    }
}