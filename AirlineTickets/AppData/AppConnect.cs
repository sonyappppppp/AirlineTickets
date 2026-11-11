using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirlineTickets.AppData
{
    class AppConnect
    {
        public static AirlineTicketOfEntities model01;
    }
    public partial class Users
    {
        public string FIO => $"{Surname} {FirstName} {Patronymic}".Trim();
    }
}
