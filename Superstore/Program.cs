namespace Superstore
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NumberRange itemsCount = new(1, 15);
            NumberRange moneysCount = new(50, 2000);
            int customersCount = 5;
            List<Item> items = new()
            {
                new("Яблоки", 150),
                new("Йогурт", 40),
                new("Молоко",90),
                new("Майонез", 100),
                new("Творог", 120),
                new("Хлеб", 50),
                new("Сыр", 200),
                new("Вода", 40),
                new("Квас", 140),
                new("Чай", 220),
                new("Кофе", 350),
                new("Огурцы", 150),
                new("Капуста", 250),
                new("Малина", 400),
                new("Колбаса", 170),
            };

            SuperstoreMenu menu = Initialize(itemsCount, moneysCount, customersCount, items);
            menu.ServeCustomers();
        }

        static SuperstoreMenu Initialize(NumberRange itemsCount, NumberRange moneysCount, int customersCount, List<Item> items)
        {
            Superstore superstore = new(items);
            CustomerRandomizer customerRandomizer = new(superstore, itemsCount, moneysCount);
            List<Customer> customers = customerRandomizer.Create(customersCount);
            superstore.TakeCustomers(customers);
            return new(superstore);
        }
    }

    public class SuperstoreMenu
    {
        private readonly Superstore _superstore;

        public SuperstoreMenu(Superstore superstore)
        {
            ArgumentNullException.ThrowIfNull(superstore);

            _superstore = superstore;
        }

        public void ServeCustomers()
        {
            while (_superstore.CustomersCount > 0)
            {
                ICustomer customer = _superstore.CurrentCustomer;
                Console.Clear();
                Console.WriteLine($"Заработанные супермаркетом деньги: {_superstore.Money}");
                Console.WriteLine($"список товаров:");
                ShowGoods();
                Console.WriteLine($"\nКоличество клиентов: {_superstore.CustomersCount}");
                Console.Write("\nДеньги клиента: ");
                ShowMoney(customer);
                Console.WriteLine("\nКорзина клиента:");
                ShowCart(customer);

                if (customer.CanPay() == false)
                {
                    Console.WriteLine("\nКлиент не может заплатить за все товары в корзине");
                }

                while (customer.CanPay() == false)
                {
                    _superstore.TellCustomerToDrop();
                    Console.WriteLine("Клиент выбросил товар");

                    if (customer.CanPay())
                    {
                        Console.WriteLine("\nКонечная корзина клиента:");
                        ShowCart(customer);
                    }
                }

                _superstore.ServeCustomer();
                Console.ReadKey();
            }
        }

        public void ShowGoods()
        {
            foreach (var good in _superstore.Goods)
            {
                Console.WriteLine($"{good.Name} ({good.Price})");
            }
        }

        public void ShowCart(ICustomer customer)
        {
            ArgumentNullException.ThrowIfNull(customer);

            List<string> itemNames = customer.Cart.GetAllNames();
            Console.WriteLine($"Цена всех товаров в корзине: {customer.GetFullPrice()}");

            foreach (var name in itemNames)
            {
                if (customer.Cart.Contains(name, out int count))
                {
                    int price = customer.Cart.GetPrice(name);

                    Console.WriteLine($"{name} - {count} ({price})");
                }
            }
        }

        public void ShowMoney(ICustomer customer)
        {
            ArgumentNullException.ThrowIfNull(customer);

            Console.WriteLine(customer.Money);
        }
    }

    public class Superstore : ISupestore
    {
        private readonly ItemFactory _factory;
        private readonly Queue<Customer> _customers;

        public Superstore(List<Item> goods)
        {
            ArgumentNullException.ThrowIfNull(goods);

            Goods = goods;
            Money = default;
            _customers = new();
            _factory = new();
        }

        public int CustomersCount => _customers.Count;

        public IReadOnlyList<Item> Goods { get; }

        public ICustomer CurrentCustomer => _customers.Peek();

        public int Money { get; private set; }

        public Item GiveGood(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Goods.Count);

            Item good = Goods[index];
            return _factory.Create(good.Name, good.Price);
        }

        public void TakeCustomers(List<Customer> customers)
        {
            ArgumentNullException.ThrowIfNull(customers);

            _customers.EnqueueRange(customers);
        }

        public void ServeCustomer()
        {
            if (CurrentCustomer.CanPay() == false)
            {
                throw new InvalidOperationException("Customer cant Pay");
            }

            Customer customer = _customers.Dequeue();
            Money += customer.Deal();
        }

        public void TellCustomerToDrop()
        {
            _customers.Peek().DropRandom();
        }
    }

    public interface ISupestore
    {
        IReadOnlyList<Item> Goods { get; }

        Item GiveGood(int index);
    }

    public class Customer : People, ICustomer
    {
        private readonly Inventory _cart;
        private readonly Random _random;

        public Customer(int money) : base(money)
        {
            _cart = new();
            _random = new();
        }

        public IInventory Cart => _cart;

        public void TakeToCart(Item item)
        {
            ArgumentNullException.ThrowIfNull(item);

            _cart.Take(item);
        }

        public void DropRandom()
        {
            List<string> allItemNames = _cart.GetAllNames();
            int index = _random.Next(allItemNames.Count);

            while (_cart.Contains(allItemNames[index]) == false)
            {
                allItemNames.RemoveAt(index);
                index = _random.Next(allItemNames.Count);
            }

            _cart.Give(allItemNames[index]);
        }

        public bool CanPay()
        {
            return Money >= GetFullPrice();
        }

        public int Deal()
        {
            int payedMoney = default;

            if (CanPay())
            {
                List<string> itemNames = _cart.GetAllNames();

                foreach (var name in itemNames)
                {
                    while (_cart.Contains(name))
                    {
                        payedMoney += BuyItem(_cart.Give(name));
                    }
                }
            }
            else
            {
                throw new ArgumentException("Customer can't pay");
            }

            return payedMoney;
        }

        public int GetFullPrice()
        {
            List<string> allItemNames = _cart.GetAllNames();
            int fullPrice = default;

            foreach (var itemName in allItemNames)
            {
                if (_cart.Contains(itemName, out int count))
                {
                    fullPrice += _cart.GetPrice(itemName) * count;
                }
            }

            return fullPrice;
        }
    }

    public interface ICustomer
    {
        int Money { get; }

        IInventory Cart { get; }

        bool CanPay();

        int GetFullPrice();
    }

    public class People
    {
        private readonly Inventory _backpack;

        public People(int money)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(money);

            Money = money;
            _backpack = new();
        }

        public int Money { get; private set; }

        protected int BuyItem(Item item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (Money >= item.Price)
            {
                Money -= item.Price;
                Take(item);
                return item.Price;
            }
            else
            {
                throw new ArgumentException("People can't buy");
            }
        }

        protected void Take(Item item)
        {
            ArgumentNullException.ThrowIfNull(item);

            _backpack.Take(item);
        }
    }

    public class Inventory : IInventory
    {
        private readonly List<Cell> _cells;

        public Inventory()
        {
            _cells = new();
        }

        public bool Contains(string itemName)
        {
            return Contains(itemName, out int _);
        }

        public bool Contains(string itemName, out int count)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(itemName);

            count = default;
            bool isContains = TryGetCell(itemName, out Cell cell) && cell.Count > 0;

            if (isContains)
            {
                count = cell.Count;
            }

            return isContains;
        }

        public int GetPrice(string itemName)
        {
            return GetCell(itemName).ItemPrice;
        }

        public List<string> GetAllNames()
        {
            List<string> names = new();

            foreach (var cell in _cells)
            {
                names.Add(cell.ItemName);
            }

            return names;
        }

        public void Take(Item item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (TryGetCell(item.Name, out Cell cell) == false)
            {
                cell = new Cell(item);
                _cells.Add(cell);
            }
            
            cell.Add();
        }

        public Item Give(string itemName)
        {
            return GetCell(itemName).Give();
        }

        private Cell GetCell(string itemName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(itemName);

            if (TryGetCell(itemName, out Cell cell) && cell.Count > 0)
            {
                return cell;
            }
            else
            {
                throw new ArgumentException($"Inventory is not contains a {itemName}");
            }
        }

        private bool TryGetCell(string itemName, out Cell cell)
        {
            Cell[] correctCells = _cells.Where(cell => cell.ItemName == itemName).ToArray();

            if (correctCells.Length > 0)
            {
                cell = correctCells[0];
                return true;
            }
            else
            {
                cell = default;
                return false;
            }
        }
    }

    public interface IInventory
    {
        bool Contains(string itemName, out int count);

        List<string> GetAllNames();

        int GetPrice(string itemName);
    }

    public class Cell
    {
        private readonly Item _item;
        private readonly ItemFactory _factory;

        public Cell(Item item)
        {
            ArgumentNullException.ThrowIfNull(item);

            _item = item;
            _factory = new();
        }

        public string ItemName => _item.Name;

        public int ItemPrice => _item.Price;

        public int Count { get; private set; }

        public void Add()
        {
            Count++;
        }

        public Item Give()
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Count);

            Count--;
            return _factory.Create(ItemName, ItemPrice);
        }
    }

    public class Item
    {
        public Item(string name, int price)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

            Name = name;
            Price = price;
        }

        public string Name { get; }

        public int Price { get; }
    }

    public class ItemFactory
    {
        public Item Create(string name, int price)
        {
            return new Item(name, price);
        }
    }

    public class CustomerFactory
    {
        public Customer Create(List<Item> items, int money)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentOutOfRangeException.ThrowIfNegative(money);

            Customer customer = new(money);

            foreach (var item in items)
            {
                customer.TakeToCart(item);
            }

            return customer;
        }
    }

    public class CustomerRandomizer
    {
        private readonly CustomerFactory _customerFactory;
        private readonly ISupestore _superstore;
        private readonly Random _random;
        private readonly NumberRange _itemsCount;
        private readonly NumberRange _moneysCount;

        public CustomerRandomizer(ISupestore superstore, NumberRange itemsCount, NumberRange moneysCount)
        {
            ArgumentNullException.ThrowIfNull(itemsCount);
            ArgumentNullException.ThrowIfNull(moneysCount);
            ArgumentNullException.ThrowIfNull(superstore);
            ArgumentOutOfRangeException.ThrowIfNegative(itemsCount.Minimum);
            ArgumentOutOfRangeException.ThrowIfNegative(moneysCount.Minimum);

            _itemsCount = itemsCount;
            _moneysCount = moneysCount;
            _customerFactory = new();
            _superstore = superstore;
            _random = new();
        }

        public List<Customer> Create(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            List<Customer> customers = new();

            for (int i = 0; i < count; i++)
            {
                int money = _random.Next(_moneysCount.Minimum, _moneysCount.Maximum);
                int itemsCount = _random.Next(_itemsCount.Minimum, _itemsCount.Maximum);
                List<Item> items = CreateRandomItems(itemsCount);
                Customer customer = _customerFactory.Create(items, money);
                customers.Add(customer);
            }

            return customers;
        }

        private List<Item> CreateRandomItems(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            List<Item> items = new();
            int itemIndex = default;

            for (int i = 0; i < count; i++)
            {
                itemIndex = _random.Next(0, _superstore.Goods.Count);
                Item item = _superstore.GiveGood(itemIndex);
                items.Add(item);
            }

            return items;
        }
    }

    public struct NumberRange
    {
        public NumberRange(int minimum, int maximum)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minimum, minimum);

            Minimum = minimum;
            Maximum = maximum;
        }

        public int Minimum { get; }

        public int Maximum { get; }
    }

    public static class QueueExtensions
    {
        public static void EnqueueRange<T>(this Queue<T> queue, List<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }
    }
}
