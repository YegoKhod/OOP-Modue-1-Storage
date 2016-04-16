using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Data_Storage
{
    /// <summary>
    /// Набор возможных ролей
    /// </summary>
    public enum Role { Insert, Update, Delete };
    /// <summary>
    /// Интерфейс определяет поведение хранилища данных
    /// </summary>
    interface IDataStorage
    {
        /// <summary>
        /// Подключение пользователя к хранилищу
        /// </summary>
        /// <param name="user">пользователь</param>
        /// <returns>Флаг true/false успеха или отказа подключения</returns>
        bool Connect(IUser user);
        /// <summary>
        /// Отключение пользователя от хранилища
        /// </summary>
        void Disconnect(IUser user);
        /// <summary>
        /// Удаляет файл из документа
        /// </summary>
        /// <param name="user">пользователь, который осуществляет операцию, должен быть авторизирован</param>
        /// <param name="Name">имя документа</param>
        /// <param name="fileName">удаляемый файл</param>
        /// <returns>Флаг успех/отказ (нет прав или не найден) удаления документа </returns>
        bool DeleteFileFromDocument(IUser user, string Name, string fileName);
        /// <summary>
        /// Удаляет документ
        /// </summary>
        /// <param name="user">пользователь, который осуществляет операцию, должен быть авторизирован</param>
        /// <param name="Name">имя документа</param>
        /// <returns>Флаг успех/отказ (нет прав или не найден) удаления документа </returns>
        bool DeleteDocument(IUser user, string Name);
        /// <summary>
        /// Обновляет файл в документе
        /// </summary>
        /// <param name="Name">имя документа</param>
        /// <param name="fileName">имя существующего файла</param>
        /// <param name="text">новое содержимое файла</param>
        /// <returns>Флаг успех/отказ (нет прав или не найден) обновления документа</returns>
        bool UpdateFileFromDocument(IUser user, string Name, string fileName, string text);
        /// <summary>
        /// Вставляет новый файл в документ, если документа нет - создает его
        /// </summary>
        /// <param name="user">пользователь, который осуществляет операцию, должен быть авторизирован</param>
        /// <param name="Name">имя документа</param>
        /// <param name="fileName">имя файла</param>
        /// <param name="text">содержимое файла</param>
        /// <returns>Флаг успех/отказ (нет прав) вставки документа</returns>
        bool InsertFileToDocument(IUser user, string Name, string fileName, string text);
    }
    /// <summary>
    /// Класс хранилища данных
    /// </summary>
    class DataStorage : IDataStorage
    {
        /// <summary>
        /// Список авторизированных пользователей
        /// </summary>
        List<IUser> Users;
        /// <summary>
        /// Список документов
        /// </summary>
        List<IDocument> Documents;
        /// <summary>
        /// Возвращает документ по имени или null
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private IDocument GetDocumentByName(string Name)
        {
            foreach (var doc in Documents)
            {
                if (doc.Name.Equals(Name))
                    return doc;
            }
            return null;
        }
        public DataStorage()
        {
            Documents = new List<IDocument>();
            Users = new List<IUser>();
        }
        public bool Connect(IUser user)
        {
            if (user.IsRole(Role.Insert) ||
                user.IsRole(Role.Delete) ||
                user.IsRole(Role.Update))
            {
                //если есть хоть одна роль то авторизируем
                Users.Add(user);
                return true;
            }
            return false;
        }
        public void Disconnect(IUser user)
        {
            if (Users.Contains(user))
                Users.Remove(user);
        }
        public bool DeleteFileFromDocument(IUser user, string Name, string fileName)
        {
            //если пользователь авторизован и если пользователь содержит роль Delete
            if (Users.Contains(user) && user.IsRole(Role.Delete))
            {
                IDocument doc = GetDocumentByName(Name);
                if (doc != null)
                {
                    if (doc.DeleteFile(fileName))  //если удаление прошло 
                    {
                        if (doc.Files.Count == 0)  //если файлов в документе нет
                            DeleteDocument(user, Name);  //удалить документ
                        return true;
                    }
                }
            }
            return false;
        }
        public bool DeleteDocument(IUser user, string Name)
        {
            if (Users.Contains(user) && user.IsRole(Role.Delete))
            {
                IDocument doc = GetDocumentByName(Name);
                if (doc != null)
                {
                    doc.Dispose();
                    Documents.Remove(doc);
                    return true;
                }
            }
            return false;
        }
        public bool InsertFileToDocument(IUser user, string Name, string fileName, string text)
        {
            if (Users.Contains(user) && user.IsRole(Role.Insert))
            {
                IDocument doc = GetDocumentByName(Name);
                if (doc == null)
                {
                    doc = new Document(Name);
                    Documents.Add(doc);
                }
                else if (doc.Files.Contains(fileName))  //если документ уже содержит файл
                    return false;   //возврат ошибки
                doc.AddFile(fileName, text);
                return true;
            }
            return false;
        }
        public bool UpdateFileFromDocument(IUser user, string Name, string fileName, string text)
        {
            //если пользователь авторизован и если пользователь содержит роль Update
            if (Users.Contains(user) && user.IsRole(Role.Update))
            {
                IDocument doc = GetDocumentByName(Name);
                if (doc != null)
                {
                    if (doc.Files.Contains(fileName))
                    {
                        doc.UpdateFile(fileName, text);
                        return true;
                    }
                }
            }
            return false;
        }
    }
    /// <summary>
    /// Интерфейс определяет поведение группы пользователей
    /// </summary>
    interface IGroup
    {
        /// <summary>
        /// Список ролей
        /// </summary>
        List<Role> Roles { get; }
        /// <summary>
        /// Добавление пользователя в группу
        /// </summary>
        /// <param name="user">пользователь</param>
        void AddUser(IUser user);
    }
    /// <summary>
    /// Абстрактный класс групп пользователей
    /// </summary>
    abstract class Group : IGroup
    {
        /// <summary>
        /// Список ролей
        /// </summary>
        public List<Role> Roles { get; private set; }
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Name">имя группы</param>
        /// <param name="roles">список ролей, переменное число параметров</param>
        public Group(params Role[] roles)
        {
            Roles = roles != null ? roles.ToList() : new List<Role>();
        }
        public void AddUser(IUser user)
        {
            user.Groups.Add(this);
        }
    }
    /// <summary>
    /// Менеджеры (все роли)
    /// </summary>
    class Managers : Group
    {
        public Managers() :
            base(Role.Update, Role.Insert, Role.Delete)
        { }
    }
    /// <summary>
    /// Гости (выборка)
    /// </summary>
    class Guests : Group
    {
        public Guests() :
            base()
        { }
    }
    /// <summary>
    /// Работники (выборка + добавление)
    /// </summary>
    class Workers : Group
    {
        public Workers() :
            base(Role.Insert)
        { }
    }
    /// <summary>
    /// Интерфейс определяет поведение пользователя
    /// </summary>
    interface IUser
    {
        /// <summary>
        /// Смена пароля
        /// </summary>
        /// <param name="oldPassword">старый пароль</param>
        /// <param name="newPassword">новый пароль</param>
        /// <returns>Флаг успех/отказ</returns>
        bool ChangePassword(string oldPassword, string newPassword);
        /// <summary>
        /// Аутентификация
        /// </summary>
        /// <param name="userName">имя пользователя</param>
        /// <param name="password">пароль</param>
        /// <returns>Флаг успех/отказ</returns>
        bool Authentification(string userName, string password);
        /// <summary>
        /// Проверка наличия роли
        /// </summary>
        /// <param name="role">роль</param>
        /// <returns>True, если пользователь имеет роль, false в противном случае</returns>
        bool IsRole(Role role);
        /// <summary>
        /// Список групп пользователя
        /// </summary>
        ICollection<IGroup> Groups { get; }
    }
    /// <summary>
    /// Абстрактный класс пользователя
    /// </summary>
    class User : IUser
    {
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string userName { get; private set; }
        /// <summary>
        /// Пароль
        /// </summary>
        string password;
        /// <summary>
        /// Список групп пользователя
        /// </summary>
        public ICollection<IGroup> Groups { get; private set; }
        /// <summary>
        /// Конструктор, вызывает регистрацию
        /// </summary>
        public User(string userName, string password)
        {
            this.userName = userName;
            this.password = password;
            Groups = new List<IGroup>();
        }
        public bool ChangePassword(string oldPassword, string newPassword)
        {
            if (oldPassword == this.password)
            {
                this.password = newPassword;
                return true;
            }
            return false;
        }
        public bool Authentification(string userName, string password)
        {
            return this.userName.Equals(userName) && this.password.Equals(password);
        }
        public bool IsRole(Role role)
        {
            foreach (var group in Groups)
                if (group.Roles.Contains(role))
                    return true;
            return false;
        }
    }
    /// <summary>
    /// Интерфейс определяет поведение документа
    /// </summary>
    interface IDocument : IDisposable
    {
        /// <summary>
        /// Идентификатор документа
        /// </summary>
        int ID { get; }
        /// <summary>
        /// Имя документа
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Файлы документа
        /// </summary>
        List<string> Files { get; }
        /// <summary>
        /// Добавление файла в документ
        /// </summary>
        /// <param name="fileName">имя нового файда</param>
        /// <param name="body">текст файла</param>
        void AddFile(string fileName, string body);
        /// <summary>
        /// Обновление файла в документе
        /// </summary>
        /// <param name="fileName">имя существующего файда</param>
        /// <param name="body">текст файла</param>
        /// <returns>Флаг успеха/отказа (например, если такого файла нет)</returns>
        bool UpdateFile(string fileName, string body);
        /// <summary>
        /// Удаление файла из документа
        /// </summary>
        /// <param name="fileName">имя существующего файда</param>
        /// <param name="body">текст файла</param>
        /// <returns>Флаг успеха/отказа (например, если такого файла нет)</returns>
        bool DeleteFile(string fileName);
        /// <summary>
        /// Удаление всех файлов документа
        /// </summary>
        new void Dispose();
    }
    class Document : IDocument, IDisposable
    {
        /// <summary>
        /// Закрытый счетчик идентификаторов документов
        /// </summary>
        static int counter = 0;
        public int ID { get; private set; }
        public string Name { get; private set; }
        public List<string> Files { get; private set; }
        /// <summary>
        /// Конструктор, создает документ по нужному пути и устанавливает счетчик
        /// </summary>
        /// <param name="Path"></param>
        public Document(string name)
        {
            ID = ++counter;
            this.Name = name;
            Files = new List<string>();
            if (!Directory.Exists(Name))
                Directory.CreateDirectory(Name);
        }
        public void AddFile(string fileName, string body)
        {
            var sw = new StreamWriter(MakeFileName(fileName));
            sw.WriteLine(body);
            sw.Close();
            Files.Add(fileName);
        }

        public bool UpdateFile(string fileName, string body)
        {
            if (Files.Equals(fileName))
            {
                string name = MakeFileName(fileName);
                if (!File.Exists(name))
                    return false;
                var sw = new StreamWriter(name);
                sw.WriteLine(body);
                sw.Close();
                return true;
            }
            return false;
        }

        private string MakeFileName(string fileName)
        {
            string file = Name + "\\" + fileName + ".data";
            return file;
        }


        public bool DeleteFile(string fileName)
        {
            if (Files.Equals(fileName))
            {
                string name = MakeFileName(fileName);
                if (File.Exists(name))
                    File.Delete(name);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            foreach (var file in Files)
            {
                string name = Name + "\\" + file;
                if (File.Exists(name))
                    File.Delete(name);
            }
            if (Directory.Exists(Name))
                Directory.Delete(Name);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            IDataStorage storage = new DataStorage();
            IGroup managers = new Managers();
            IGroup workers = new Workers();
            IGroup guests = new Guests();

            IUser user1 = new User("user1", "qwerty");
            IUser user2 = new User("user2", "12345");
            IUser user3 = new User("user3", "qwerty");
            IUser user4 = new User("user4", "qwerty");

            managers.AddUser(user1);
            workers.AddUser(user2);
            guests.AddUser(user3);
            managers.AddUser(user4);

            if (storage.Connect(user1))
                Console.WriteLine("Connect OK");
            else
                Console.WriteLine("Connect FAIL");
            if (storage.Connect(user2))
                Console.WriteLine("Connect OK");
            else
                Console.WriteLine("Connect FAIL");
            if (storage.Connect(user3))    //группа Guests не имеет прав
                Console.WriteLine("Connect OK");
            else
                Console.WriteLine("Connect FAIL");

            if (storage.InsertFileToDocument(user1, "Doc1", "file1", "Text from file1 of document1"))
                Console.WriteLine("Insert OK");
            else
                Console.WriteLine("Insert FAIL");
            if (storage.InsertFileToDocument(user3, "Doc2", "file1", "Text from file1 of document2")) //нет доступа
                Console.WriteLine("Insert OK");
            else
                Console.WriteLine("Insert FAIL");
            storage.Disconnect(user2);     //разлогиниваем
            if (storage.InsertFileToDocument(user2, "Doc2", "file1", "Text from file1 of document2")) //разлогинен
                Console.WriteLine("Insert OK");
            else
                Console.WriteLine("Insert FAIL");
            storage.Connect(user2);
            if (storage.InsertFileToDocument(user2, "Doc2", "file1", "Text from file1 of document2")) //ОК
                Console.WriteLine("Insert OK");
            else
                Console.WriteLine("Insert FAIL");

            if (storage.DeleteDocument(user2, "Doc2")) //нет доступа
                Console.WriteLine("Delete OK");
            else
                Console.WriteLine("Delete FAIL");

            if (storage.UpdateFileFromDocument(user1, "Doc1", "file1", "New text from fil1 of document1)")) //ОК
                Console.WriteLine("Update OK");
            else
                Console.WriteLine("Update FAIL");


            storage.Disconnect(user1);
            storage.Disconnect(user3);
            Console.ReadKey();
        }
    }
}
