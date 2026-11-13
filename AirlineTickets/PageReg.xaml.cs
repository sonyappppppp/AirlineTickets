using AirlineTickets.AppData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AirlineTickets
{
    public partial class PageReg : Page
    {
        public PageReg()
        {
            InitializeComponent();
        }

        // ============================================================
        // 1. ОБРАБОТЧИКИ СОБЫТИЙ
        // ============================================================
        private void Field_TextChanged(object sender, TextChangedEventArgs e) => ValidateAll();
        private void Password_TextChanged(object sender, RoutedEventArgs e) => ValidateAll();

        // Запрет ввода всего, кроме цифр (для телефона)
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // Маска телефона: автоматически делает +7 (XXX) XXX-XX-XX
        private void Phone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (digits.StartsWith("8") && digits.Length > 1) digits = "7" + digits.Substring(1);

                string f = "";
                if (digits.Length > 0) f += "+7";
                if (digits.Length > 1) f += " (" + digits.Substring(1, Math.Min(3, digits.Length - 1));
                if (digits.Length >= 4) f += ") " + digits.Substring(4, Math.Min(3, digits.Length - 4));
                if (digits.Length >= 7) f += "-" + digits.Substring(7, Math.Min(2, digits.Length - 7));
                if (digits.Length >= 9) f += "-" + digits.Substring(9, Math.Min(2, digits.Length - 9));

                if (tb.Text != f)
                {
                    int c = tb.CaretIndex;
                    tb.Text = f;
                    tb.CaretIndex = Math.Min(c, f.Length);
                }
            }
            ValidateAll();
        }

        // ============================================================
        // 2. ПРОВЕРКА КАЖДОГО ПОЛЯ
        // ============================================================
        private bool ValidateName()
        {
            string val = txtname.Text.Trim();
            bool ok = Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]{2,}$");
            SetFieldState(txtname, errName, ok, ok ? "" : "Минимум 2 буквы");
            return ok;
        }

        private bool ValidateSurname()
        {
            string val = txtsurn.Text.Trim();
            bool ok = Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]{2,}$");
            SetFieldState(txtsurn, errSurn, ok, ok ? "" : "Минимум 2 буквы");
            return ok;
        }

        private bool ValidatePatronymic()
        {
            string val = txtpatr.Text.Trim();
            // Отчество необязательно, но если введено, должно быть корректным
            if (string.IsNullOrEmpty(val))
            {
                SetFieldState(txtpatr, errPatr, true, "");
                return true;
            }
            bool ok = Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]{2,}$");
            SetFieldState(txtpatr, errPatr, ok, ok ? "" : "Только буквы");
            return ok;
        }

        private bool ValidateLogin()
        {
            string val = txtlog.Text.Trim();
            bool ok = Regex.IsMatch(val, @"^[A-Za-z0-9._\-]{3,30}$");

            if (ok && AppConnect.model01.Users.Any(u => u.Login == val))
            {
                SetFieldState(txtlog, errLog, false, "Этот логин уже занят");
                return false;
            }

            SetFieldState(txtlog, errLog, ok, ok ? "" : "3-30 символов (буквы, цифры, _ или -)");
            return ok;
        }

        private bool ValidateEmail()
        {
            string val = txtemail.Text.Trim();
            bool ok = false;
            try { ok = (new System.Net.Mail.MailAddress(val).Address == val); } catch { }
            SetFieldState(txtemail, errEmail, ok, ok ? "" : "Некорректный email");
            return ok;
        }

        private bool ValidatePhone()
        {
            string val = txtphon.Text.Trim();
            string digits = new string(val.Where(char.IsDigit).ToArray());
            bool ok = digits.Length == 11 && (digits.StartsWith("7") || digits.StartsWith("8"));
            SetFieldState(txtphon, errPhone, ok, ok ? "" : "Введите 11 цифр");
            return ok;
        }

        private bool ValidatePassword()
        {
            string val = passw.Password;
            bool hasLetter = Regex.IsMatch(val, @"[A-Za-zА-Яа-яЁё]");
            bool hasDigit = Regex.IsMatch(val, @"\d");
            bool ok = val.Length >= 6 && hasLetter && hasDigit;

            string errMsg = "";
            if (val.Length < 6) errMsg = "Минимум 6 символов";
            else if (!hasLetter || !hasDigit) errMsg = "Нужны буква и цифра";

            SetFieldState(passw, errPass, ok, ok ? "" : errMsg);

            // Если пароль изменился, перепроверяем подтверждение
            if (passwcheck.Password.Length > 0) ValidatePasswordConfirm();

            return ok;
        }

        private bool ValidatePasswordConfirm()
        {
            bool ok = passw.Password == passwcheck.Password && passwcheck.Password.Length > 0;
            SetFieldState(passwcheck, errPassCheck, ok, ok ? "" : "Пароли не совпадают");
            return ok;
        }

        // ============================================================
        // 3. УПРАВЛЕНИЕ ВИЗУАЛЬНЫМ СОСТОЯНИЕМ (Адаптировано под тёмную тему)
        // ============================================================
        private void SetFieldState(Control control, TextBlock errBlock, bool isValid, string errorMessage)
        {
            if (isValid)
            {
                errBlock.Visibility = Visibility.Collapsed;
                control.BorderBrush = Brushes.Green; // Зелёная рамка при успехе
            }
            else
            {
                errBlock.Text = errorMessage;
                errBlock.Visibility = Visibility.Visible;
                control.BorderBrush = Brushes.Red; // Красная рамка при ошибке
            }
        }

        // ============================================================
        // 4. ФИНАЛЬНАЯ ПРОВЕРКА И РЕГИСТРАЦИЯ
        // ============================================================
        private void ValidateAll()
        {
            bool allValid = ValidateName() &&
                            ValidateSurname() &&
                            ValidatePatronymic() &&
                            ValidateLogin() &&
                            ValidateEmail() &&
                            ValidatePhone() &&
                            ValidatePassword() &&
                            ValidatePasswordConfirm();

            // Кнопка активна только когда всё заполнено верно
            btnreg.IsEnabled = allValid;
        }

        private void btnreg_Click(object sender, RoutedEventArgs e)
        {
            if (!btnreg.IsEnabled) return;

            try
            {
                var newUser = new Users
                {
                    FirstName = txtname.Text.Trim(),
                    Surname = txtsurn.Text.Trim(),
                    Patronymic = string.IsNullOrWhiteSpace(txtpatr.Text) ? null : txtpatr.Text.Trim(),
                    Login = txtlog.Text.Trim(),
                    Password = passw.Password, // В реальном проекте здесь должен быть хэш (например, BCrypt)
                    Email = txtemail.Text.Trim(),
                    Phone = txtphon.Text.Trim(),
                    RoleId = 4 // 2 = Пользователь (или 1 = Гость, проверь по своей БД)
                };

                AppConnect.model01.Users.Add(newUser);
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно! Теперь вы можете войти.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Переход на страницу входа
                NavigationService.Navigate(new PagAuth());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при регистрации: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}





