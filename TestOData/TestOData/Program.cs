using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace TestOData
{
    class Program
    {
        private const string serverUri = "http://localhost:82/0/ServiceModel/EntityDataService.svc/";
        private const string authServiceUtri = "http://localhost:82/ServiceModel/AuthService.svc/Login";
        private static readonly XNamespace ds = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace dsmd = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace atom = "http://www.w3.org/2005/Atom";
        
        static void Main(string[] args)
        {
            SwitchCont();
        }

        public static void SwitchCont()
        {
            Console.WriteLine("Выберите действие: 1 - Вывести средства связи контакта, \n 2 - Добавить средство связи контакту, \n 3 - Удалить средство связи контакта");
            switch (Console.ReadLine())
            {
                case "1":
                    Console.WriteLine("Введите имя контакта");
                    string Name = Console.ReadLine();
                    ContactCommunication("Supervisor", "Supervisor", Name);
                    SwitchCont();
                    break;
                case "2":
                    Console.WriteLine("Введите ключ контакта");
                    string IdC = Console.ReadLine();
                    Console.WriteLine("Введите ключ средства связи");
                    string IdT = Console.ReadLine();
                    Console.WriteLine("Введите значение типа связи");
                    string Nd = Console.ReadLine();
                    AddCommunication(IdT, IdC, Nd);
                    SwitchCont();
                    break;
                case "3":
                    Console.WriteLine("Введите ключ типа связи контакта, который хотите удалить");
                    string IdTD = Console.ReadLine();
                    DeleteCommunication(IdTD);
                    SwitchCont();
                    break;
                default:
                    Console.WriteLine("Что-то пошло не так");
                    break;

            }
        }
       
        public static void ContactCommunication(string userName, string userPassword, string ContName)
        {
            var authRequest = HttpWebRequest.Create(authServiceUtri) as HttpWebRequest;
            authRequest.Method = "POST";
            authRequest.ContentType = "application/json";
            var bpmCookieContainer = new CookieContainer();
            authRequest.CookieContainer = bpmCookieContainer;
            using (var requestStream = authRequest.GetRequestStream())
            {
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(@"{
                                ""UserName"":""" + userName + @""",
                                ""UserPassword"":""" + userPassword + @""",
                                ""SolutionName"":""TSBpm"",
                                ""TimeZoneOffset"":-120,
                                ""Language"":""Ru-ru""
                                }");
                }
            }
            using (var response = (HttpWebResponse)authRequest.GetResponse())
            {
                var dataRequest = HttpWebRequest.Create(serverUri + "ContactCommunicationCollection?$filter=Contact/Name eq '"+ContName+"'") as HttpWebRequest;
                dataRequest.Method = "GET";
                dataRequest.CookieContainer = bpmCookieContainer;
                using (var dataResponse = (HttpWebResponse)dataRequest.GetResponse())
                {
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                    var contacts = from entry in xmlDoc.Descendants(atom + "entry")
                                   select new
                                   {
                                       Communication = entry.Element(atom + "content")
                                               .Element(dsmd + "properties")
                                               .Element(ds + "Number").Value
                                   };
                    
                    foreach (var contact in contacts)
                    {
                        Console.WriteLine(contact);
                    }
                    Console.ReadKey();
                }
               
            }

        }
        public static void AddCommunication(string IdCont, string IDType, string Numbel)
        {
            var content = new XElement(dsmd + "properties",
                          new XElement(ds + "CommunicationTypeId", new Guid(IdCont)),
                          new XElement(ds + "ContactId", new Guid(IDType)),
                          new XElement(ds + "Number", Numbel));
            var entry = new XElement(atom + "entry",
                        new XElement(atom + "content",
                        new XAttribute("type", "application/xml"), content));
            var request = (HttpWebRequest)HttpWebRequest.Create(serverUri + "ContactCommunicationCollection/");
            request.Credentials = new NetworkCredential("Supervisor", "Supervisor");
            request.Method = "POST";
            request.Accept = "application/atom+xml";
            request.ContentType = "application/atom+xml;type=entry";
            using (var writer = XmlWriter.Create(request.GetRequestStream()))
            {
                entry.WriteTo(writer);
            }
            using (WebResponse response = request.GetResponse())
            {
                if (((HttpWebResponse)response).StatusCode == HttpStatusCode.Created)
                {
                    Console.WriteLine("Средство связи добавлено");
                    Console.ReadKey();
                }
            }
        }
        public static void DeleteCommunication(string IdDel)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(serverUri
                    + "ContactCommunicationCollection(guid'" + IdDel + "')");
            request.Credentials = new NetworkCredential("Supervisor", "Supervisor");
            request.Method = "DELETE";
            request.Accept = "application/atom+xml";
            request.ContentType = "application/atom+xml;type=entry";
            using (WebResponse response = request.GetResponse())
            {
                Console.WriteLine("Средство связи удалено");
                Console.ReadKey();
            }
        }
    }
}
