namespace Superstore
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NumberRange goodsCount = new(1, 15);
            NumberRange moneysCount = new(50, 2000);
            int customersCount = 5;
            List<Good> goods = new()
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

            SuperstoreMenu menu = Initialize(goodsCount, moneysCount, customersCount, goods);
            menu.ServeCustomers();
        }

        static SuperstoreMenu Initialize(NumberRange goodsCount, NumberRange moneysCount, int customersCount, List<Good> Goods)
        {
            Superstore superstore = new(Goods);
            CustomerFactory customerRandomizer = new(superstore, goodsCount, moneysCount);
            List<Customer> customers = customerRandomizer.CreateRandomCustomers(customersCount);
            superstore.TakeCustomers(customers);
            return new(superstore);
        }
    }

    public static class UserUtilits
    {
        private static Random _random = new();

        public static int GenerateRandomNumber(NumberRange range)
        {
            return _random.Next(range.Minimum, range.Maximum);
        }

        public static int GenerateRandomNumber(int maximum)
        {
            return _random.Next(maximum);
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
                ShowSuperstoreGoods();
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

        public void ShowSuperstoreGoods()
        {
            foreach (var good in _superstore.Goods)
            {
                Console.WriteLine($"{good.Name} ({good.Price})");
            }
        }

        public void ShowCart(ICustomer customer)
        {
            ArgumentNullException.ThrowIfNull(customer);

            List<string> goodNames = customer.Cart.GetAllNames();
            Console.WriteLine($"Цена всех товаров в корзине: {customer.GetFullPrice()}");

            foreach (var name in goodNames)
            {
                if (customer.Cart.IsContains(name, out int count))
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
        private readonly Queue<Customer> _customers;

        public Superstore(List<Good> goods)
        {
            ArgumentNullException.ThrowIfNull(goods);

            Goods = goods;
            Money = default;
            _customers = new();
        }

        public int CustomersCount => _customers.Count;

        public IReadOnlyList<Good> Goods { get; }

        public ICustomer CurrentCustomer => _customers.Peek();

        public int Money { get; private set; }

        public Good GiveGood(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Goods.Count);

            Good good = Goods[index];
            return good.Clone();
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
                throw new InvalidOperationException("Customer can't Pay");
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
        IReadOnlyList<Good> Goods { get; }

        Good GiveGood(int index);
    }

    public class Customer : ICustomer
    {
        private readonly Inventory _cart;
        private readonly Inventory _backpack;

        public Customer(List<Good> goods, int money)
        {
            ArgumentNullException.ThrowIfNull(goods);
            ArgumentOutOfRangeException.ThrowIfNegative(money);

            _cart = new();
            _backpack = new();
            Money = money;

            foreach (var good in goods)
            {
                _cart.Take(good);
            }
        }

        public IInventory Cart => _cart;

        public int Money { get; private set; }

        public void DropRandom()
        {
            List<string> allItemNames = _cart.GetAllNames();
            int index = UserUtilits.GenerateRandomNumber(allItemNames.Count);

            while (_cart.IsContains(allItemNames[index]) == false)
            {
                allItemNames.RemoveAt(index);
                index = UserUtilits.GenerateRandomNumber(allItemNames.Count);
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
                List<string> goodNames = _cart.GetAllNames();

                foreach (var name in goodNames)
                {
                    while (_cart.IsContains(name))
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
            List<string> allGoodNames = _cart.GetAllNames();
            int fullPrice = default;

            foreach (var goodName in allGoodNames)
            {
                if (_cart.IsContains(goodName, out int count))
                {
                    fullPrice += _cart.GetPrice(goodName) * count;
                }
            }

            return fullPrice;
        }

        private int BuyItem(Good good)
        {
            ArgumentNullException.ThrowIfNull(good);

            if (Money >= good.Price)
            {
                Money -= good.Price;
                Take(good);
                return good.Price;
            }
            else
            {
                throw new ArgumentException("People can't buy");
            }
        }

        private void Take(Good good)
        {
            ArgumentNullException.ThrowIfNull(good);

            _backpack.Take(good);
        }
    }

    public interface ICustomer
    {
        int Money { get; }

        IInventory Cart { get; }

        bool CanPay();

        int GetFullPrice();
    }

    public class Inventory : IInventory
    {
        private readonly List<Cell> _cells;

        public Inventory()
        {
            _cells = new();
        }

        public bool IsContains(string goodName)
        {
            return IsContains(goodName, out int _);
        }

        public bool IsContains(string goodName, out int count)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(goodName);

            count = default;
            bool isContains = TryGetCell(goodName, out Cell cell) && cell.Count > 0;

            if (isContains)
            {
                count = cell.Count;
            }

            return isContains;
        }

        public int GetPrice(string goodName)
        {
            return GetCell(goodName).GoodPrice;
        }

        public List<string> GetAllNames()
        {
            List<string> names = new();

            foreach (var cell in _cells)
            {
                names.Add(cell.GoodName);
            }

            return names;
        }

        public void Take(Good good)
        {
            ArgumentNullException.ThrowIfNull(good);

            if (TryGetCell(good.Name, out Cell cell) == false)
            {
                cell = new Cell(good);
                _cells.Add(cell);
            }
            
            cell.Add();
        }

        public Good Give(string goodName)
        {
            return GetCell(goodName).Give();
        }

        private Cell GetCell(string goodName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(goodName);

            if (TryGetCell(goodName, out Cell cell) && cell.Count > 0)
            {
                return cell;
            }
            else
            {
                throw new ArgumentException($"Inventory is not contains a {goodName}");
            }
        }

        private bool TryGetCell(string goodName, out Cell cell)
        {
            Cell[] correctCells = _cells.Where(cell => cell.GoodName == goodName).ToArray();

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
        bool IsContains(string goodName, out int count);

        List<string> GetAllNames();

        int GetPrice(string goodName);
    }

    public class Cell
    {
        private readonly Good _good;

        public Cell(Good good)
        {
            ArgumentNullException.ThrowIfNull(good);

            _good = good;
        }

        public string GoodName => _good.Name;

        public int GoodPrice => _good.Price;

        public int Count { get; private set; }

        public void Add()
        {
            Count++;
        }

        public Good Give()
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Count);

            Count--;
            return _good.Clone();
        }
    }

    public class Good
    {
        public Good(string name, int price)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

            Name = name;
            Price = price;
        }

        public string Name { get; }

        public int Price { get; }

        public Good Clone() => new Good(Name, Price);
    }

    public class CustomerFactory
    {
        private readonly ISupestore _superstore;
        private readonly NumberRange _goodCount;
        private readonly NumberRange _moneysCount;

        public CustomerFactory(ISupestore superstore, NumberRange goodsCount, NumberRange moneysCount)
        {
            ArgumentNullException.ThrowIfNull(goodsCount);
            ArgumentNullException.ThrowIfNull(moneysCount);
            ArgumentNullException.ThrowIfNull(superstore);
            ArgumentOutOfRangeException.ThrowIfNegative(goodsCount.Minimum);
            ArgumentOutOfRangeException.ThrowIfNegative(moneysCount.Minimum);

            _goodCount = goodsCount;
            _moneysCount = moneysCount;
            _superstore = superstore;
        }

        public List<Customer> CreateRandomCustomers(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            List<Customer> customers = new();

            for (int i = 0; i < count; i++)
            {
                int money = UserUtilits.GenerateRandomNumber(_moneysCount);
                int goodsCount = UserUtilits.GenerateRandomNumber(_goodCount);
                List<Good> goods = GiveRandomGoods(goodsCount);
                Customer customer = new(goods, money);
                customers.Add(customer);
            }

            return customers;
        }

        private List<Good> GiveRandomGoods(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            List<Good> goods = new();
            int goodIndex = default;

            for (int i = 0; i < count; i++)
            {
                goodIndex = UserUtilits.GenerateRandomNumber(_superstore.Goods.Count);
                Good good = _superstore.GiveGood(goodIndex);
                goods.Add(good);
            }

            return goods;
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
