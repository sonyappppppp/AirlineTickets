using AirlineTickets;
using AirlineTickets.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AirlineTickets
{
    /// <summary>
    /// Логика взаимодействия для PagAuth.xaml
    /// </summary>
    public partial class PagAuth : Page
    {
        private bool isLoginOk = false;
        private bool isPasswordOk = false;
        private bool isRepeatOk = false;
        public PagAuth()
        {
            InitializeComponent();
            UpdateButtonState();
        }
   
            // Обработчики ввода
            private void TxtLogin_TextChanged(object sender, TextChangedEventArgs e) => ValidateLogin();

            private void PassPassword_PasswordChanged(object sender, RoutedEventArgs e)
            {
                ValidatePassword();
                CheckRepeatMatch();
            }

            private void PassRepeat_PasswordChanged(object sender, RoutedEventArgs e) => CheckRepeatMatch();

            // Валидация логина
            private void ValidateLogin()
            {
                string val = txtLogin.Text.Trim();
                if (string.IsNullOrWhiteSpace(val))
                {
                    SetError(txtLogin, errLogin, "Введите логин");
                    isLoginOk = false;
                }
                else
                {
                    ClearError(txtLogin, errLogin);
                    isLoginOk = true;
                }
                UpdateButtonState();
            }

            // Валидация пароля
            private void ValidatePassword()
            {
                string val = passPassword.Password;
                if (string.IsNullOrEmpty(val))
                {
                    SetError(passPassword, errPassword, "Введите пароль");
                    isPasswordOk = false;
                }
                else
                {
                    ClearError(passPassword, errPassword);
                    isPasswordOk = true;
                }
                UpdateButtonState();
            }

            // Проверка совпадения паролей
            private void CheckRepeatMatch()
            {
                string pass1 = passPassword.Password;
                string pass2 = passRepeat.Password;

                if (string.IsNullOrEmpty(pass2))
                {
                    SetError(passRepeat, errRepeat, "Повторите пароль");
                    isRepeatOk = false;
                }
                else if (pass1 != pass2)
                {
                    SetError(passRepeat, errRepeat, "Пароли не совпадают");
                    isRepeatOk = false;
                }
                else
                {
                    ClearError(passRepeat, errRepeat);
                    isRepeatOk = true;
                }
                UpdateButtonState();
            }

            // Показать ошибку
            private void SetError(Control control, TextBlock errorBlock, string message)
            {
                control.BorderBrush = Brushes.Red;
                errorBlock.Text = message;
                errorBlock.Visibility = Visibility.Visible;
            }

            // Убрать ошибку
            private void ClearError(Control control, TextBlock errorBlock)
            {
                control.BorderBrush = SystemColors.ControlDarkBrush;
                errorBlock.Text = string.Empty;
                errorBlock.Visibility = Visibility.Collapsed;
            }

            // Обновить состояние кнопки
            private void UpdateButtonState()
            {
                btnAuth.IsEnabled = isLoginOk && isPasswordOk && isRepeatOk;
            }

            // Кнопка "Войти"
            private void BtnAuth_Click(object sender, RoutedEventArgs e)
            {
          
            try
            {
                string login = txtLogin.Text.Trim();
                string password = passPassword.Password;

                var user = AppConnect.model01.Users
                    .FirstOrDefault(u => u.Login == login && u.Password == password);

                if (user != null)
                {
                    // Сохраняем пользователя ДО навигации
                    AppConnect.u = user;

                    MessageBox.Show($"Добро пожаловать, {user.FirstName}!", "Успех",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    NavigationService.Navigate(new PageTicket());
                }
                else
                {
                    SetError(txtLogin, errLogin, "Неверный логин или пароль");
                    SetError(passPassword, errPassword, "Неверный логин или пароль");
                    txtLogin.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
           
        

            // Ссылка на регистрацию
            private void HyperlinkRegister_Click(object sender, RoutedEventArgs e)
            {
                 AppData.AppFrame.Framemain.Navigate(new PageReg());
            }
        }
    }