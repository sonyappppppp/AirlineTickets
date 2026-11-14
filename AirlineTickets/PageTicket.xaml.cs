using AirlineTickets.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace AirlineTickets
{
    public partial class PageTicket : Page
    {
        private List<FlightCard> _allFlights = new List<FlightCard>();

        // ID ролей из вашей БД (судя по скриншоту)
        private const int ROLE_ADMIN = 1;      // Администратор
        private const int ROLE_GUEST = 2;      // Гость
        private const int ROLE_MANAGER = 3;    // Менеджер
        private const int ROLE_USER = 4;       // Пользователь

        public PageTicket()
        {
            InitializeComponent();

            var mw = Application.Current.MainWindow as MainWindow;
            if (mw?.fio != null)
                mw.fio.Text = AppConnect.u?.FIO ?? "Гость";

            SetupRoleBasedUI();
            LoadFlights();
        }

        // ============================================================
        // НАСТРОЙКА ИНТЕРФЕЙСА ПО РОЛЯМ (ТЗ F2)
        // ============================================================
        private void SetupRoleBasedUI()
        {
            int roleId = AppConnect.u?.RoleId ?? ROLE_GUEST;
            bool isGuest = (roleId == ROLE_GUEST);
            bool canManage = (roleId == ROLE_ADMIN || roleId == ROLE_MANAGER);

            // Гость не видит фильтры и кнопку бронирования
            filterPanel.Visibility = isGuest ? Visibility.Collapsed : Visibility.Visible;

            // Кнопка "Добавить рейс" видна только Менеджеру и Админу
            if (btnAddFlight != null)
                btnAddFlight.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadFlights()
        {
            try
            {
                int roleId = AppConnect.u?.RoleId ?? ROLE_GUEST;
                bool canManage = (roleId == ROLE_ADMIN || roleId == ROLE_MANAGER);

                var rawData = (from f in AppConnect.model01.Flights
                               join depAirport in AppConnect.model01.Airports on f.DepartureAirportId equals depAirport.Id
                               join depCity in AppConnect.model01.City on depAirport.CityId equals depCity.CityId
                               join arrAirport in AppConnect.model01.Airports on f.ArrivalAirportId equals arrAirport.Id
                               join arrCity in AppConnect.model01.City on arrAirport.CityId equals arrCity.CityId
                               join airline in AppConnect.model01.Airlines on f.AirlineId equals airline.Id
                               join flightType in AppConnect.model01.FlightTypes on f.FlightTypeId equals flightType.Id into ftGroup
                               from ft in ftGroup.DefaultIfEmpty()
                               join seat in AppConnect.model01.FlightSeats on f.Id equals seat.FlightId into seatGroup
                               where f.DepartureTime >= DateTime.Today
                               select new
                               {
                                   f.Id,
                                   f.FlightNumber,
                                   AirlineName = airline.AirlineName,
                                   AirlineIATA = airline.IATACode,
                                   DepartureCity = depCity.City1,
                                   DepartureIATA = depAirport.IATACode,
                                   f.DepartureTime,
                                   ArrivalCity = arrCity.City1,
                                   ArrivalIATA = arrAirport.IATACode,
                                   f.ArrivalTime,
                                   f.Duration,
                                   f.BasePrice,
                                   FlightType = ft != null ? ft.TypeName : "Не указан",
                                   AvailableSeats = seatGroup.Sum(s => (int?)s.AvailableSeats) ?? 0,
                                   FirstSeatClassId = seatGroup.Min(s => (int?)s.SeatClassId)
                               }).ToList();

                _allFlights = rawData.Select(r => new FlightCard
                {
                    FlightId = r.Id,
                    FlightNumber = r.FlightNumber,
                    AirlineName = r.AirlineName,
                    AirlineIATA = r.AirlineIATA,
                    DepartureCity = r.DepartureCity,
                    DepartureIATA = r.DepartureIATA,
                    DepartureTime = r.DepartureTime,
                    ArrivalCity = r.ArrivalCity,
                    ArrivalIATA = r.ArrivalIATA,
                    ArrivalTime = r.ArrivalTime,
                    DurationMinutes = r.Duration,
                    DurationText = r.Duration.HasValue ? $"{r.Duration.Value / 60} ч {r.Duration.Value % 60} мин" : "—",
                    FlightType = r.FlightType,
                    Price = r.BasePrice,
                    AvailableSeats = r.AvailableSeats,
                    TicketDescription = GetClassName(r.FirstSeatClassId),

                    // ⬇️ Кнопки "Изменить/Удалить" видны только Менеджеру и Админу ⬇️
                    AdminControlsVisibility = canManage ? Visibility.Visible : Visibility.Collapsed
                }).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рейсов:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetClassName(int? classId)
        {
            switch (classId)
            {
                case 1: return "Эконом";
                case 2: return "Комфорт";
                case 3: return "Бизнес";
                case 4: return "Первый класс";
                default: return "Эконом";
            }
        }

        private void Filters_Changed(object sender, EventArgs e)
        {
            if (cmbSort == null || cmbFilter == null || txtSearch == null) return;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (listFlights == null) return;
            string search = txtSearch?.Text?.Trim().ToLower() ?? "";
            string sort = (cmbSort?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string filter = (cmbFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

            IEnumerable<FlightCard> result = _allFlights;

            if (!string.IsNullOrEmpty(search))
                result = result.Where(f => (f.DepartureCity?.ToLower().Contains(search) == true) || (f.ArrivalCity?.ToLower().Contains(search) == true) || (f.FlightNumber?.ToLower().Contains(search) == true) || (f.AirlineName?.ToLower().Contains(search) == true));

            switch (filter)
            {
                case "Только прямые": result = result.Where(f => f.FlightType == "Прямой" || f.FlightType == "Регулярный"); break;
                case "С пересадкой": result = result.Where(f => f.FlightType == "С пересадкой"); break;
                case "Чартер": result = result.Where(f => f.FlightType == "Чартерный"); break;
            }

            switch (sort)
            {
                case "По цене (возрастание)": result = result.OrderBy(f => f.Price); break;
                case "По цене (убывание)": result = result.OrderByDescending(f => f.Price); break;
                case "По времени вылета (ранние)": result = result.OrderBy(f => f.DepartureTime); break;
                case "По времени вылета (поздние)": result = result.OrderByDescending(f => f.DepartureTime); break;
                case "По длительности": result = result.OrderBy(f => f.DurationMinutes ?? int.MaxValue); break;
            }

            var list = result.ToList();
            listFlights.ItemsSource = list;
            // if (txtEmpty != null) txtEmpty.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ============================================================
        // ДЕЙСТВИЯ ПОЛЬЗОВАТЕЛЯ
        // ============================================================
        private void Book_Click(object sender, RoutedEventArgs e)
        {
            // 1. Получаем выбранную карточку рейса
            var selectedCard = listFlights.SelectedItem as FlightCard;

            if (selectedCard == null)
            {
                MessageBox.Show("Сначала выберите рейс в списке.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Инициализируем корзину, если она вдруг null
            if (AppConnect.Cart == null)
            {
                AppConnect.Cart = new List<CartItem>();
            }

            // 3. Проверяем, есть ли уже этот рейс в корзине
            var existingItem = AppConnect.Cart.FirstOrDefault(c => c.Offer.FlightId == selectedCard.FlightId);

            if (existingItem != null)
            {
                // Если есть, увеличиваем количество мест
                existingItem.Quantity++;
                MessageBox.Show($"Количество мест на рейс {selectedCard.FlightNumber} увеличено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // 4. Если нет, создаем новый элемент корзины, преобразуя данные
                var newItem = new CartItem
                {
                    Offer = new FlightOffer
                    {
                        FlightId = selectedCard.FlightId,
                        AirlineName = selectedCard.AirlineName,
                        FlightNumber = selectedCard.FlightNumber,
                        DepartureCity = selectedCard.DepartureCity,
                        DepartureIATA = selectedCard.DepartureIATA,
                        DepartureTime = selectedCard.DepartureTime,
                        ArrivalCity = selectedCard.ArrivalCity,
                        ArrivalIATA = selectedCard.ArrivalIATA,
                        ArrivalTime = selectedCard.ArrivalTime,
                        DurationText = selectedCard.DurationText,
                        Price = selectedCard.Price,
                        AvailableSeats = selectedCard.AvailableSeats,
                        SeatClassId = 1, // По умолчанию Эконом (можно доработать при выборе класса)
                        TicketDescription = selectedCard.TicketDescription
                    },
                    Quantity = 1
                };

                AppConnect.Cart.Add(newItem);
                MessageBox.Show($"Рейс {selectedCard.FlightNumber} добавлен в корзину!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // 5. Только после добавления переходим на страницу корзины
            NavigationService.Navigate(new PageCart());
        }

        private void MyBookings_Click(object sender, RoutedEventArgs e) => NavigationService.Navigate(new PageCart());
        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            if (AppConnect.u == null)
            {
                MessageBox.Show("Требуется авторизация.", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NavigationService.Navigate(new PagAuth());
                return;
            }
            NavigationService.Navigate(new PageProfile());
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из системы?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AppConnect.u = null;
                NavigationService.Navigate(new PagAuth());
            }
        }

        // ============================================================
        // CRUD ДЛЯ МЕНЕДЖЕРА / АДМИНА (ТЗ F2)
        // ============================================================
        private void AddFlight_Click(object sender, RoutedEventArgs e)
        {
            // Переход на страницу добавления (нужно создать PageAddEditTicket.xaml)
            NavigationService.Navigate(new PageAddEditTicket(null));
        }

        private void EditFlight_Click(object sender, RoutedEventArgs e)
        {
            int flightId = (int)((Button)sender).Tag;
            var flight = _allFlights.FirstOrDefault(f => f.FlightId == flightId);
            if (flight != null)
            {
                NavigationService.Navigate(new PageAddEditTicket(flight));
            }
        }

        private void DeleteFlight_Click(object sender, RoutedEventArgs e)
        {
            int roleId = AppConnect.u?.RoleId ?? ROLE_GUEST;

            // ⬇️ ТЗ F2: Менеджеру ЗАПРЕЩЕНО удаление записей ⬇️
            if (roleId == ROLE_MANAGER)
            {
                MessageBox.Show("Менеджеру запрещено удалять записи. Обратитесь к Администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Удалять может только Администратор
            if (roleId != ROLE_ADMIN) return;

            int flightId = (int)((Button)sender).Tag;
            var flight = _allFlights.FirstOrDefault(f => f.FlightId == flightId);

            var result = MessageBox.Show($"Вы уверены, что хотите удалить рейс {flight?.FlightNumber}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var dbFlight = AppConnect.model01.Flights.FirstOrDefault(f => f.Id == flightId);
                if (dbFlight != null)
                {
                    AppConnect.model01.Flights.Remove(dbFlight);
                    AppConnect.model01.SaveChanges();
                    MessageBox.Show("Рейс успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadFlights();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении (возможно, на рейс есть активные бронирования): " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cart_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new PageCart());
        }
    }

    // ============================================================
    // DTO КАРТОЧКИ РЕЙСА
    // ============================================================
    public class FlightCard
    {
        public int FlightId { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string AirlineIATA { get; set; }
        public string DepartureCity { get; set; }
        public string DepartureIATA { get; set; }
        public DateTime DepartureTime { get; set; }
        public string ArrivalCity { get; set; }
        public string ArrivalIATA { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string DurationText { get; set; }
        public string FlightType { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public string TicketDescription { get; set; }

        // ⬇️ ОБЯЗАТЕЛЬНО ДОБАВЬТЕ ЭТО СВОЙСТВО ⬇️
        public Visibility AdminControlsVisibility { get; set; } = Visibility.Collapsed;
    }
}