using AirlineTickets.AppData;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AirlineTickets
{
    public partial class PageAddEditTicket : Page
    {
        private FlightCard _editingFlight;

        public PageAddEditTicket(FlightCard flightToEdit)
        {
            InitializeComponent();
            _editingFlight = flightToEdit;

            // Загрузка справочников в ComboBox
            cmbAirline.ItemsSource = AppConnect.model01.Airlines.ToList();
            cmbDepAirport.ItemsSource = AppConnect.model01.Airports.ToList();
            cmbArrAirport.ItemsSource = AppConnect.model01.Airports.ToList();

            if (_editingFlight != null)
            {
                txtTitle.Text = "Редактировать рейс";
                txtFlightNumber.Text = _editingFlight.FlightNumber;
                cmbAirline.SelectedValue = AppConnect.model01.Airlines.FirstOrDefault(a => a.AirlineName == _editingFlight.AirlineName)?.Id;
                cmbDepAirport.SelectedValue = AppConnect.model01.Airports.FirstOrDefault(a => a.IATACode == _editingFlight.DepartureIATA)?.Id;
                cmbArrAirport.SelectedValue = AppConnect.model01.Airports.FirstOrDefault(a => a.IATACode == _editingFlight.ArrivalIATA)?.Id;
                dpDepDate.SelectedDate = _editingFlight.DepartureTime.Date;
                txtDepTime.Text = _editingFlight.DepartureTime.ToString("HH:mm");
                txtDuration.Text = _editingFlight.DurationMinutes?.ToString();
                txtPrice.Text = _editingFlight.Price.ToString();
            }
            else
            {
                dpDepDate.SelectedDate = DateTime.Today.AddDays(1);
            }
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtFlightNumber.Text) || cmbDepAirport.SelectedValue == null || cmbArrAirport.SelectedValue == null)
                {
                    MessageBox.Show("Заполните все обязательные поля.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime depDate = dpDepDate.SelectedDate ?? DateTime.Today;
                if (!TimeSpan.TryParse(txtDepTime.Text, out TimeSpan depTime))
                {
                    MessageBox.Show("Неверный формат времени вылета (используйте ЧЧ:ММ).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime departureTime = depDate.Date + depTime;
                int duration = int.Parse(txtDuration.Text);
                decimal price = decimal.Parse(txtPrice.Text);
                int airlineId = (int)cmbAirline.SelectedValue;
                int depAirportId = (int)cmbDepAirport.SelectedValue;
                int arrAirportId = (int)cmbArrAirport.SelectedValue;

                if (_editingFlight != null)
                {
                    // РЕДАКТИРОВАНИЕ
                    var dbFlight = AppConnect.model01.Flights.FirstOrDefault(f => f.Id == _editingFlight.FlightId);
                    if (dbFlight != null)
                    {
                        dbFlight.FlightNumber = txtFlightNumber.Text.Trim();
                        dbFlight.AirlineId = airlineId;
                        dbFlight.DepartureAirportId = depAirportId;
                        dbFlight.ArrivalAirportId = arrAirportId;
                        dbFlight.DepartureTime = departureTime;
                        dbFlight.ArrivalTime = departureTime.AddMinutes(duration);
                        dbFlight.Duration = duration;
                        dbFlight.BasePrice = price;
                    }
                }
                else
                {
                    // ДОБАВЛЕНИЕ
                    var newFlight = new Flights
                    {
                        FlightNumber = txtFlightNumber.Text.Trim(),
                        AirlineId = airlineId,
                        DepartureAirportId = depAirportId,
                        ArrivalAirportId = arrAirportId,
                        DepartureTime = departureTime,
                        ArrivalTime = departureTime.AddMinutes(duration),
                        Duration = duration,
                        BasePrice = price,
                        FlightTypeId = 1 // По умолчанию "Прямой"
                    };
                    AppConnect.model01.Flights.Add(newFlight);

                    // Сразу добавляем места (Эконом: 100 мест)
                    AppConnect.model01.FlightSeats.Add(new FlightSeats
                    {
                        FlightId = newFlight.Id, // EF сам подставит после SaveChanges, но лучше сделать 2 шага или использовать хранимку. 
                        // Для простоты: сначала сохраним рейс, потом добавим места.
                    });
                }

                AppConnect.model01.SaveChanges();

                // Если это новый рейс, добавляем для него места (после получения Id)
                if (_editingFlight == null)
                {
                    var justAdded = AppConnect.model01.Flights.OrderByDescending(f => f.Id).FirstOrDefault();
                    if (justAdded != null)
                    {
                        AppConnect.model01.FlightSeats.Add(new FlightSeats
                        {
                            FlightId = justAdded.Id,
                            SeatClassId = 1, // Эконом
                            TotalSeats = 100,
                            AvailableSeats = 100
                        });
                        AppConnect.model01.SaveChanges();
                    }
                }

                MessageBox.Show("Рейс успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}