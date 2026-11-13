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
    public partial class PageProfile : Page
    {
        public PageProfile()
        {
            InitializeComponent();
            Loaded += PageProfile_Loaded;
        }

        private void PageProfile_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка авторизации
            if (AppConnect.u == null)
            {
                MessageBox.Show("Требуется авторизация.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.Navigate(new PagAuth());
                return;
            }

            // Загружаем данные пользователя
            txtSurname.Text = AppConnect.u.Surname ?? "";
            txtName.Text = AppConnect.u.FirstName ?? "";
            txtPatronymic.Text = AppConnect.u.Patronymic ?? "";
            txtLogin.Text = AppConnect.u.Login ?? "";
            txtEmail.Text = AppConnect.u.Email ?? "";
            txtPhone.Text = AppConnect.u.Phone ?? "";

            // Отображаем роль
            int roleId = AppConnect.u.RoleId ?? 0;
            string roleName = "";
            switch (roleId)
            {
                case 1: roleName = "Администратор"; break;
                case 2: roleName = "Гость"; break;
                case 3: roleName = "Менеджер"; break;
                case 4: roleName = "Пользователь"; break;
                default: roleName = "Неизвестно"; break;
            }
            txtRole.Text = roleName;

            ValidateAll();
        }

        // ============================================================
        // ОБРАБОТЧИКИ СОБЫТИЙ
        // ============================================================
        private void Field_TextChanged(object sender, TextChangedEventArgs e) => ValidateAll();
        private void Password_TextChanged(object sender, RoutedEventArgs e) => ValidateAll();

        // Запрет ввода букв в телефон
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // ============================================================
        // ВАЛИДАЦИЯ ПОЛЕЙ
        // ============================================================
        private bool ValidateSurname()
        {
            string val = txtSurname.Text.Trim();
            bool ok = Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]{2,}$");
            SetFieldState(txtSurname, errSurname, ok, ok ? "" : "Минимум 2 буквы");
            return ok;
        }

        private bool ValidateName()
        {
            string val = txtName.Text.Trim();
            bool ok = Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]{2,}$");
            SetFieldState(txtName, errName, ok, ok ? "" : "Минимум 2 буквы");
            return ok;
        }

        private bool ValidatePatronymic()
        {
            string val = txtPatronymic.Text.Trim();
            if (string.IsNullOrEmpty(val))
            {
                SetFieldState(txtPatronymic, errPatronymic, true, "");
                return true;
            }
            bool ok = Regex.IsMatch(val, @"^[A-Za-zА-Яа-яЁё\s\-]{2,}$");
            SetFieldState(txtPatronymic, errPatronymic, ok, ok ? "" : "Только буквы");
            return ok;
        }

        private bool ValidateEmail()
        {
            string val = txtEmail.Text.Trim();
            bool ok = false;
            try { ok = (new System.Net.Mail.MailAddress(val).Address == val); } catch { }
            SetFieldState(txtEmail, errEmail, ok, ok ? "" : "Некорректный email");
            return ok;
        }

        private bool ValidatePhone()
        {
            string val = txtPhone.Text.Trim();
            string digits = new string(val.Where(char.IsDigit).ToArray());
            bool ok = digits.Length == 11 && (digits.StartsWith("7") || digits.StartsWith("8"));
            SetFieldState(txtPhone, errPhone, ok, ok ? "" : "Введите 11 цифр (например, +7 999 123-45-67)");
            return ok;
        }

        private bool ValidatePassword()
        {
            string val = passNewPassword.Password;
            string confirm = passConfirmPassword.Password;

            // Если оба поля пустые — пароль не меняется, это ок
            if (string.IsNullOrEmpty(val) && string.IsNullOrEmpty(confirm))
            {
                SetFieldState(passNewPassword, errPassword, true, "");
                SetFieldState(passConfirmPassword, errPasswordConfirm, true, "");
                return true;
            }

            // Проверяем новый пароль
            bool hasLetter = Regex.IsMatch(val, @"[A-Za-zА-Яа-яЁё]");
            bool hasDigit = Regex.IsMatch(val, @"\d");
            bool ok = val.Length >= 6 && hasLetter && hasDigit;

            string errMsg = "";
            if (val.Length < 6) errMsg = "Минимум 6 символов";
            else if (!hasLetter || !hasDigit) errMsg = "Нужны буква и цифра";

            SetFieldState(passNewPassword, errPassword, ok, ok ? "" : errMsg);

            // Проверяем совпадение
            bool matchOk = val == confirm && confirm.Length > 0;
            SetFieldState(passConfirmPassword, errPasswordConfirm, matchOk, matchOk ? "" : "Пароли не совпадают");

            return ok && matchOk;
        }

        private void SetFieldState(Control control, TextBlock errBlock, bool isValid, string errorMessage)
        {
            if (isValid)
            {
                errBlock.Visibility = Visibility.Collapsed;
                control.BorderBrush = Brushes.Green;
            }
            else
            {
                errBlock.Text = errorMessage;
                errBlock.Visibility = Visibility.Visible;
                control.BorderBrush = Brushes.Red;
            }
        }

        private void ValidateAll()
        {
            bool allValid = ValidateSurname() &&
                            ValidateName() &&
                            ValidatePatronymic() &&
                            ValidateEmail() &&
                            ValidatePhone() &&
                            ValidatePassword();

            btnSave.IsEnabled = allValid;
        }

        // ============================================================
        // СОХРАНЕНИЕ ИЗМЕНЕНИЙ
        // ============================================================
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!btnSave.IsEnabled) return;

            try
            {
                // Обновляем данные пользователя
                AppConnect.u.Surname = txtSurname.Text.Trim();
                AppConnect.u.FirstName = txtName.Text.Trim();
                AppConnect.u.Patronymic = string.IsNullOrWhiteSpace(txtPatronymic.Text) ? null : txtPatronymic.Text.Trim();
                AppConnect.u.Email = txtEmail.Text.Trim();
                AppConnect.u.Phone = txtPhone.Text.Trim();

                // Если введён новый пароль — обновляем его
                if (!string.IsNullOrEmpty(passNewPassword.Password))
                {
                    AppConnect.u.Password = passNewPassword.Password;
                }

                // Сохраняем в БД
                AppConnect.model01.SaveChanges();

                MessageBox.Show("Данные успешно обновлены!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Обновляем ФИО в шапке главного окна
                var mw = Application.Current.MainWindow as MainWindow;
                if (mw?.fio != null)
                    mw.fio.Text = AppConnect.u.FIO;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
