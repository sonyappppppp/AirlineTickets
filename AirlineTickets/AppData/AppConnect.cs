using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace AirlineTickets.AppData
{
    class AppConnect
    {
        public static AirlineTicketOfEntities2 model01;
        public static Users u;
        public static System.Collections.Generic.List<CartItem> Cart = new System.Collections.Generic.List<CartItem>();
         

            // ВАЖНО: Тип должен быть List<CartItem>
          
        


    }
    
    public partial class Users
    {
        
        public string FIO => $"{Surname} {FirstName} {Patronymic}".Trim();
    }
}
