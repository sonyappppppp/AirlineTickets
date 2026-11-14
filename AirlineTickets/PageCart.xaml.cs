using AirlineTickets.AppData;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AirlineTickets
{
    public partial class PageCart : Page
    {
        // Локальный список корзины (ссылка на AppConnect.Cart)
        private List<CartItem> _cart;
        private List<HistoryItem> _history = new List<HistoryItem>();

        public PageCart()
        {
            InitializeComponent();
            Loaded += PageCart_Loaded;
        }

        private void PageCart_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка авторизации
            if (AppConnect.u == null)
            {
                MessageBox.Show("Для просмотра корзины необходимо войти в систему.",
                    "Требуется авторизация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                NavigationService.Navigate(new PagAuth());
                return;
            }

            // Инициализация корзины из глобального хранилища
            if (AppConnect.Cart == null)
                AppConnect.Cart = new List<CartItem>();
            _cart = AppConnect.Cart;

            RefreshCart();
            LoadHistory();
            ValidateForm();
        }

        // ============================================================
        // КОРЗИНА
        // ============================================================
        private void RefreshCart()
        {
            listCart.ItemsSource = _cart;
            listCart.Visibility = _cart.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtCartEmpty.Visibility = _cart.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

            decimal total = _cart.Sum(c => c.LineTotal);
            txtTotal.Text = "Итого: " + total.ToString("N0") + " ₽";
            txtTotal.Visibility = _cart.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ValidateForm();
        }

        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as CartItem;
            if (item == null) return;

            if (item.Quantity > 1)
                item.Quantity--;
            else
                _cart.Remove(item);

            RefreshCart();
        }

        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as CartItem;
            if (item == null) return;

            if (item.Quantity < item.Offer.AvailableSeats)
                item.Quantity++;
            else
                MessageBox.Show("Больше нет свободных мест на этот рейс.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);

            RefreshCart();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement)?.DataContext as CartItem;
            if (item == null) return;

            _cart.Remove(item);
            RefreshCart();
        }

        // ============================================================
        // ИСТОРИЯ ЗАКАЗОВ
        // ============================================================
        private void LoadHistory()
        {
            try
            {
                int userId = AppConnect.u.Id;

                string sql = @"
                    SELECT 
                        b.Id AS BookingId,
                        b.BookingNumber,
                        b.BookingDate,
                        b.TotalAmount,
                        s.StatusName,
                        STUFF((
                            SELECT '; ' + f2.FlightNumber + ' ' + depA2.AirportName + ' → ' + arrA2.AirportName
                            FROM BookingItems bi2
                            INNER JOIN Flights f2 ON bi2.FlightId = f2.Id
                            INNER JOIN Airports depA2 ON f2.DepartureAirportId = depA2.Id
                            INNER JOIN Airports arrA2 ON f2.ArrivalAirportId = arrA2.Id
                            WHERE bi2.BookingId = b.Id
                            FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 2, 2000, '') AS RouteText
                    FROM Bookings b
                    INNER JOIN Status s ON b.StatusId = s.StatusId
                    WHERE b.UserId = @userId
                    ORDER BY b.BookingDate DESC";

                SqlParameter param = new SqlParameter("@userId", userId);
                var rows = AppConnect.model01.Database
                    .SqlQuery<HistoryRow>(sql, param)
                    .ToList();

                _history = rows.Select(r => new HistoryItem
                {
                    BookingNumber = r.BookingNumber,
                    DateText = r.BookingDate.ToString("dd.MM.yyyy HH:mm"),
                    RouteText = string.IsNullOrEmpty(r.RouteText) ? "—" : r.RouteText,
                    Status = r.StatusName ?? "Неизвестно",
                    TotalText = (r.TotalAmount ?? 0).ToString("N0") + " ₽"
                }).ToList();

                listHistory.ItemsSource = _history;
                listHistory.Visibility = _history.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                txtHistoryEmpty.Visibility = _history.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки истории: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // ВАЛИДАЦИЯ ФОРМЫ ПАССАЖИРА
        // ============================================================
        private void Field_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private bool IsFormValid()
        {
            string fio = txtFullName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string ser = txtPassportSeries.Text.Trim();
            string num = txtPassportNumber.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (fio.Length < 3 || !Regex.IsMatch(fio, @"^[A-Za-zА-Яа-яЁё\s\-]+$")) return false;

            var phoneDigits = Regex.Replace(phone, @"\D", "");
            if (phoneDigits.Length != 11) return false;
            if (!phoneDigits.StartsWith("7") && !phoneDigits.StartsWith("8")) return false;

            if (!Regex.IsMatch(ser, @"^\d{4}$")) return false;
            if (!Regex.IsMatch(num, @"^\d{6}$")) return false;

            if (string.IsNullOrEmpty(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email) return false;
            }
            catch { return false; }

            return true;
        }

        private void ValidateForm()
        {
            btnCheckout.IsEnabled = _cart != null && _cart.Count > 0 && IsFormValid();
        }

        // ============================================================
        // ОФОРМЛЕНИЕ ЗАКАЗА
        // ============================================================
        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
         

            // ... внутри вашего метода обработки кнопки (например, BtnBook_Click)

            if (AppConnect.u == null)
            {
                MessageBox.Show("Пользователь не авторизован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_cart == null || _cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Создаем список для сбора всех ошибок
            List<string> errors = new List<string>();

            // 2. Проверяем ФИО
            string fio = txtFullName.Text.Trim();
            bool fioOk = Regex.IsMatch(fio, @"^[A-Za-zА-Яа-яЁё\s\-]{3,}$");
            errFio.Visibility = fioOk ? Visibility.Collapsed : Visibility.Visible;
            if (!fioOk) errors.Add("• ФИО: минимум 3 буквы, только буквы, пробелы и дефис.");

            // 3. Проверяем Телефон
            string phoneDigits = new string(txtPhone.Text.Where(char.IsDigit).ToArray());
            bool phoneOk = phoneDigits.Length == 11 && (phoneDigits.StartsWith("7") || phoneDigits.StartsWith("8"));
            errPhone.Visibility = phoneOk ? Visibility.Collapsed : Visibility.Visible;
            if (!phoneOk) errors.Add("• Телефон: должен содержать ровно 11 цифр и начинаться с 7 или 8.");

            // 4. Проверяем Серию паспорта
            bool seriesOk = Regex.IsMatch(txtPassportSeries.Text.Trim(), @"^\d{4}$");
            errSeries.Visibility = seriesOk ? Visibility.Collapsed : Visibility.Visible;
            if (!seriesOk) errors.Add("• Серия паспорта: должна состоять ровно из 4 цифр.");

            // 5. Проверяем Номер паспорта
            bool numberOk = Regex.IsMatch(txtPassportNumber.Text.Trim(), @"^\d{6}$");
            errNumber.Visibility = numberOk ? Visibility.Collapsed : Visibility.Visible;
            if (!numberOk) errors.Add("• Номер паспорта: должен состоять ровно из 6 цифр.");

            // 6. Проверяем Email
            bool emailOk = false;
            string email = txtEmail.Text.Trim();
            try
            {
                var addr = new MailAddress(email);
                emailOk = addr.Address == email; // Проверка на корректность формата
            }
            catch
            {
                emailOk = false;
            }
            errEmail.Visibility = emailOk ? Visibility.Collapsed : Visibility.Visible;
            if (!emailOk) errors.Add("• Email: введен некорректный формат адреса.");

            // 7. Если есть ошибки, показываем их все сразу и прерываем выполнение
            if (errors.Count > 0)
            {
                string errorMessage = "Пожалуйста, исправьте следующие ошибки:\n\n" + string.Join("\n", errors);
                MessageBox.Show(errorMessage, "Ошибка валидации данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Прерываем метод, бронирование не продолжается
            }


            try
            {
                decimal total = _cart.Sum(c => c.LineTotal);
                string bookingNumber = "BK-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                string payment = (cmbPayment.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Банковская карта";

                // 1. Создаём бронирование
                var booking = new Bookings
                {
                    BookingNumber = bookingNumber,
                    BookingDate = DateTime.Now,
                    UserId = AppConnect.u.Id,
                    TotalAmount = total,
                    StatusId = 2 // Ожидает оплаты
                };
                AppConnect.model01.Bookings.Add(booking);
                AppConnect.model01.SaveChanges();

                // 2. Создаём позиции + уменьшаем места
                foreach (var item in _cart)
                {
                    // Ищем Ticket по классу обслуживания
                    int ticketId = GetTicketIdByClass(item.Offer.SeatClassId);

                    var bi = new BookingItems
                    {
                        BookingId = booking.Id,
                        FlightId = item.Offer.FlightId,
                        TicketId = ticketId,
                        PassengerFullName = txtFullName.Text.Trim(),
                        PassengerDocument = txtPassportSeries.Text.Trim() + " " + txtPassportNumber.Text.Trim(),
                        SeatNumber = "A" + item.Quantity,
                        Quantity = item.Quantity,
                        PricePerUnit = item.Offer.Price
                    };
                    AppConnect.model01.BookingItems.Add(bi);

                    // Уменьшаем свободные места
                    string upd = @"
                        UPDATE FlightSeats 
                        SET AvailableSeats = CASE 
                            WHEN AvailableSeats - @qty < 0 THEN 0 
                            ELSE AvailableSeats - @qty END
                        WHERE FlightId = @fid AND SeatClassId = @sid";

                    AppConnect.model01.Database.ExecuteSqlCommand(upd,
                        new SqlParameter("@qty", item.Quantity),
                        new SqlParameter("@fid", item.Offer.FlightId),
                        new SqlParameter("@sid", item.Offer.SeatClassId));
                }

                AppConnect.model01.SaveChanges();

                // 3. Запись оплаты
                var paymentRec = new Payments
                {
                    BookingId = booking.Id,
                    PaymentDate = DateTime.Now,
                    Amount = total,
                    PaymentMethod = payment
                };
                AppConnect.model01.Payments.Add(paymentRec);
                AppConnect.model01.SaveChanges();

                // 4. Очищаем корзину
                _cart.Clear();
                RefreshCart();
                LoadHistory();

                MessageBox.Show("Заказ №" + bookingNumber + " успешно оформлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Получаем Id тарифа по классу обслуживания
        private int GetTicketIdByClass(int seatClassId)
        {
            try
            {
                string sql = "SELECT TOP 1 Id FROM Tickets WHERE SeatClassId = @sid ORDER BY Id";
                var id = AppConnect.model01.Database
                    .SqlQuery<int>(sql, new SqlParameter("@sid", seatClassId))
                    .FirstOrDefault();
                return id == 0 ? 2 : id; // по умолчанию Эконом Стандарт
            }
            catch
            {
                return 2;
            }
        }
        // 1. Запрет ввода букв (работает для телефона и паспорта)
        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // 2. Простая маска телефона (автоматически делает +7 (XXX) XXX-XX-XX)
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

                if (tb.Text != f) { int c = tb.CaretIndex; tb.Text = f; tb.CaretIndex = Math.Min(c, f.Length); }
            }
            ValidateForm();
        }

        // 3. Проверка полей и показ/скрытие ошибок
       
        

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageTicket());
        }

        private void listCart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }


    // ============================================================
    // КЛАССЫ ДЛЯ КОРЗИНЫ (public, чтобы видеть из AppConnect)
    // ============================================================
    public class FlightOffer
    {
        public int FlightId { get; set; }
        public string AirlineName { get; set; }
        public string FlightNumber { get; set; }
        public string DepartureCity { get; set; }
        public string DepartureIATA { get; set; }
        public DateTime DepartureTime { get; set; }
        public string ArrivalCity { get; set; }
        public string ArrivalIATA { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string DurationText { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public int SeatClassId { get; set; }
        public string TicketDescription { get; set; }
    }

    public class CartItem
    {
        public FlightOffer Offer { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal LineTotal
        {
            get { return Offer != null ? Offer.Price * Quantity : 0; }
        }
    }

    public class HistoryItem
    {
        public string BookingNumber { get; set; }
        public string DateText { get; set; }
        public string RouteText { get; set; }
        public string Status { get; set; }
        public string TotalText { get; set; }
    }

    // Вспомогательный класс для SQL-истории
    public class HistoryRow
    {
        public int BookingId { get; set; }
        public string BookingNumber { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string StatusName { get; set; }
        public string RouteText { get; set; }
    }
}