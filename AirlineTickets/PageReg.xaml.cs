using AirlineTickets.AppData;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml.Linq;

namespace AirlineTickets
{
    /// <summary>
    /// Логика взаимодействия для PageReg.xaml
    /// </summary>
    public partial class PageReg : Page
    {
        
        public PageReg()
        {
            InitializeComponent();
        }
        private void passwcheck_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (passw.Password != passwcheck.Password)
            {
                btnreg.IsEnabled = false;
                passwcheck.Background = Brushes.LightCoral;
                passwcheck.BorderBrush = Brushes.Red;
            }
            else
            {
                btnreg.IsEnabled = true;
                passwcheck.Background = Brushes.LightGreen;
                passwcheck.BorderBrush = Brushes.Green;
            }
        }
        private void btnreg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Финальная проверка валидности всех полей
                if (!IsAllValid())
                {
                    MessageBox.Show("Заполните все поля корректно!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Проверка уникальности логина через EF
                if (AppConnect.model01.Users.Any(u => u.Login == txtlog.Text.Trim()))
                {
                    txtlog.Background = Brushes.LightCoral;
                    MessageBox.Show("Пользователь с таким логином уже существует!",
                                    "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 3. Проверка уникальности email через EF
                if (AppConnect.model01.Users.Any(u => u.Email == txtemail.Text.Trim()))
                {
                    txtemail.Background = Brushes.LightCoral;
                    MessageBox.Show("Этот Email уже зарегистрирован!",
                                    "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 4. Создаём нового пользователя
                Users userObj = new Users()
                {
                    Surname = txtsurn.Text.Trim(),
                    FirstName = txtname.Text.Trim(),
                    Patronymic = string.IsNullOrWhiteSpace(txtpatr.Text) ? null : txtpatr.Text.Trim(),
                    Login = txtlog.Text.Trim(),
                    Password = passw.Password,
                    Email = txtemail.Text.Trim(),
                    Phone = txtphon.Text.Trim(),
                    RoleId = 2   // "Пользователь"
                };

                AppConnect.model01.Users.Add(userObj);
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно!", "Уведомление",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                AppData.AppFrame.Framemain.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // ВАЛИДАЦИЯ ПОЛЕЙ (подсветка фона)
        // ============================================================

        private void ValidateName(TextBox tb)
        {
            string val = tb.Text.Trim();
            if (string.IsNullOrWhiteSpace(val) || val.Length < 2 ||
                !Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]+$"))
            {
                tb.Background = Brushes.LightCoral;
                tb.BorderBrush = Brushes.Red;
            }
            else
            {
                tb.Background = Brushes.LightGreen;
                tb.BorderBrush = Brushes.Green;
            }
        }

        private void ValidateNameOptional(TextBox tb)
        {
            string val = tb.Text.Trim();
            if (string.IsNullOrEmpty(val))
            {
                tb.Background = Brushes.White;
                tb.BorderBrush = SystemColors.ControlDarkBrush;
                return;
            }
            if (val.Length < 2 || !Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]+$"))
            {
                tb.Background = Brushes.LightCoral;
                tb.BorderBrush = Brushes.Red;
            }
            else
            {
                tb.Background = Brushes.LightGreen;
                tb.BorderBrush = Brushes.Green;
            }
        }

        private void ValidateLogin(TextBox tb)
        {
            string val = tb.Text.Trim();
            if (string.IsNullOrWhiteSpace(val) || val.Length < 3 || val.Length > 30 ||
                !Regex.IsMatch(val, @"^[A-Za-z0-9._\-]+$"))
            {
                tb.Background = Brushes.LightCoral;
                tb.BorderBrush = Brushes.Red;
            }
            else
            {
                tb.Background = Brushes.LightGreen;
                tb.BorderBrush = Brushes.Green;
            }
        }

        private void ValidateEmail(TextBox tb)
        {
            string val = tb.Text.Trim();
            bool ok = false;
            try
            {
                var addr = new MailAddress(val);
                ok = (addr.Address == val);
            }
            catch { ok = false; }

            if (!ok)
            {
                tb.Background = Brushes.LightCoral;
                tb.BorderBrush = Brushes.Red;
            }
            else
            {
                tb.Background = Brushes.LightGreen;
                tb.BorderBrush = Brushes.Green;
            }
        }

        private void ValidatePhone(TextBox tb)
        {
            string val = tb.Text.Trim();
            var digits = Regex.Replace(val, @"\D", "");
            if (digits.Length != 11 || !digits.StartsWith("7"))
            {
                tb.Background = Brushes.LightCoral;
                tb.BorderBrush = Brushes.Red;
            }
            else
            {
                tb.Background = Brushes.LightGreen;
                tb.BorderBrush = Brushes.Green;
            }
        }

        private void ValidatePassword(PasswordBox pb)
        {
            string val = pb.Password;
            if (string.IsNullOrEmpty(val) || val.Length < 6 ||
                !Regex.IsMatch(val, @"[A-Za-zА-Яа-яЁё]") ||
                !Regex.IsMatch(val, @"\d"))
            {
                pb.Background = Brushes.LightCoral;
                pb.BorderBrush = Brushes.Red;
            }
            else
            {
                pb.Background = Brushes.LightGreen;
                pb.BorderBrush = Brushes.Green;
            }
        }

        // ============================================================
        // ФИНАЛЬНАЯ ПРОВЕРКА — все ли поля валидны
        // ============================================================
        private bool IsAllValid()
        {
            return txtname.Background == Brushes.LightGreen &&
                   txtsurn.Background == Brushes.LightGreen &&
                   txtlog.Background == Brushes.LightGreen &&
                   txtemail.Background == Brushes.LightGreen &&
                   txtphon.Background == Brushes.LightGreen &&
                   passw.Background == Brushes.LightGreen &&
                   passwcheck.Background == Brushes.LightGreen;
        }
        private void Txtname_TextChanged(object sender, TextChangedEventArgs e) => ValidateName(txtname);
        private void Txtsurn_TextChanged(object sender, TextChangedEventArgs e) => ValidateName(txtsurn);
        private void Txtpatr_TextChanged(object sender, TextChangedEventArgs e) => ValidateNameOptional(txtpatr);
        private void Txtlog_TextChanged(object sender, TextChangedEventArgs e) => ValidateLogin(txtlog);
        private void Txtemail_TextChanged(object sender, TextChangedEventArgs e) => ValidateEmail(txtemail);
        private void Txtphon_TextChanged(object sender, TextChangedEventArgs e) => ValidatePhone(txtphon);
        private void Passw_PasswordChanged(object sender, RoutedEventArgs e) => ValidatePassword(passw);

    }
}


       

       
            