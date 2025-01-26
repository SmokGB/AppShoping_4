using AppShoping.Components.csvReader;
using AppShoping.Components.DataProviders;
using AppShoping.Components.DataProviders.Extensions;
using AppShoping.Data.Entities;
using AppShoping.Data.Repositories;
using AppShoping.Data.Repositories.Extensions;
using AppShoping.Components.csvReader.Models;
using System.Xml.Linq;

namespace AppShoping.App.Menu;

public class UserCommunication : IUserCommunication
{
    private readonly IRepository<Food> _foodRepository;
    private readonly IPurchaseProvider _purchaseProvider;
    private readonly IRepository<PurchaseStatistics> _purchaseRepository;
    private readonly ICsvReader _csvReader;

    private const string AuditDataPath = "Audit.txt";



    private List<string> shopMenu = new()
    {
        "1 - Produkty do zakupu",
        "2 - Dodaj produkt",
        "3 - Dodaj produkt bio",
        "4 - Usuń produkt z listy zakupów",
        "5 - Statystyki zakupów",
        "6 - LinQ/export XML",
        "7 - Wyjście z programu"
    };

    public UserCommunication(IRepository<Food> foodRepository,
        IPurchaseProvider purchaseProvider,
        IRepository<PurchaseStatistics> purchaseRepository,
        ICsvReader csvReader
      )
    {


        _foodRepository = foodRepository;
        _purchaseProvider = purchaseProvider;
        _purchaseRepository = purchaseRepository;
        _csvReader = csvReader;

        _foodRepository.ItemAdded += OnWriteToFile;
        _foodRepository.ProductDeleted += OnProductDeleted;

        try
        {
            _foodRepository.ImportFoodListFromJson();
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.Message}");
        }

    }

    private void OnProductDeleted(object? sender, Food? e)
    {
        using (var write = File.AppendText(AuditDataPath))
        {
            write.WriteLine($"{DateTime.Now} usunięto : {e?.ProductName} z  {e?.GetType().Name}");
        }
    }

    private void OnWriteToFile(object? sender, Food? e)
    {
        using (var write = File.AppendText(AuditDataPath))
        {
            write.WriteLine($"{DateTime.Now} dodano : {e?.ProductName} z {e?.GetType().Name}");
        }
    }

    public void DisplayMenu()
    {
        Console.Clear();
        Console.WriteLine("Wykaz zakupów do zrobienia\n");

        foreach (var i in shopMenu)
            Console.WriteLine(i);

        Console.WriteLine("\n------------- Wybierz opcję [1- 6] ---------------\n");
    }

    private void Alert()
    {
        Console.WriteLine("Niewłaściwy wybór w Menu");
        Console.ReadKey();
    }

    private void Paginate<T>(Func<IEnumerable<T>> dataProvider, string sectionTitle)
    {
        int currentPage = 1;
        const int pageSize = 5;
        var totalItems = dataProvider().Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        while (true)
        {
            Console.Clear();
            Console.WriteLine($"<<---- {sectionTitle} ---->>\n");

            ShowCurrentPageItems(dataProvider, currentPage, pageSize);

            DisplayPaginationInfo(currentPage, totalPages);

            if (!HandleNavigation(ref currentPage, totalPages))
                break;
        }
    }

    private void ShowCurrentPageItems<T>(Func<IEnumerable<T>> dataProvider, int currentPage, int pageSize)
    {
        var itemsToShow = dataProvider()
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var item in itemsToShow)
        {
            Console.WriteLine(item);
        }
    }


    private void DisplayPaginationInfo(int currentPage, int totalPages)
    {
        Console.WriteLine($"\nStrona {currentPage} z {totalPages}");
        Console.WriteLine("Naciśnij 'P' aby przejść do poprzedniej strony, 'N' aby przejść do następnej strony, lub 'E' aby wyjść.");
    }

    private bool HandleNavigation(ref int currentPage, int totalPages)
    {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.P && currentPage > 1)
        {
            currentPage--;
            return true;
        }
        else if (key == ConsoleKey.N && currentPage < totalPages)
        {
            currentPage++;
            return true;
        }
        else if (key == ConsoleKey.E)
        {
            return false;
        }

        return true;
    }


    private void Paginate<T>(Func<IEnumerable<T[]>> dataProvider, string sectionTitle)
    {
        int currentPage = 1;
        const int pageSize = 1;
        var totalChunks = dataProvider().Count();
        var totalPages = (int)Math.Ceiling((double)totalChunks / pageSize);

        while (true)
        {
            Console.Clear();
            Console.WriteLine($"<---- {sectionTitle} ---->\n");

            ShowCurrentChunks(dataProvider, currentPage);

            DisplayPaginationInfo(currentPage, totalPages);

            if (!HandleNavigation(ref currentPage, totalPages))
                break;
        }
    }

    private void ShowCurrentChunks<T>(Func<IEnumerable<T[]>> dataProvider, int currentPage)
    {
        var chunksToShow = dataProvider()
            .Skip((currentPage - 1) * 1)
            .Take(1)
            .ToList();

        foreach (var chunk in chunksToShow)
        {
            Console.WriteLine($"Chunk {currentPage}");
            foreach (var item in chunk)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine("<------->");
        }
    }


    private void ShowStatisctic()
    {
        Console.WriteLine("<---- Statystyki zakupionych produktów ---->\n");

        Paginate(() => _purchaseProvider.GetProductsList()!, "Wykaz zakupionych produktów");

        Paginate(() => _purchaseProvider.GetUniqueProduct()!, "Lista unikalnych produktów");

        Paginate(() => _purchaseProvider.OrderProductByNameShopAndPrice()!, "Lista zakupionych produktów wg sklepu i ceny");

        DisplayMinPurchasedProduct();

        DisplayMaxPurchasedProduct();

        Paginate(() => _purchaseProvider.WhereNameShopIsAndPromotionIs("Auchan", true), "Lista zakupionych produktów na promocji w sklepie Auchan");

        DisplayFirstProductByPrice(7.20M);

        Paginate(() => PurchaseExtensions.OrderByNameAndPrice(_purchaseRepository.GetAll()), $"\n METODA ROZSZERZAJĄCA<--Lista Produktów po cenie i nazwie-->");

        Paginate(() => _purchaseProvider.WhereNameShopIsAndPromotionIs("Auchan", true), "Lista zakupionych produktów na promocji w sklepie Auchan");
        Paginate(() => _purchaseProvider.TakeProduct(0..10), "Pierwszych 10 produktów");
        Paginate(() => _purchaseProvider.SkipProduct(10), "Lista produktów z pominięciem pierwszych 10-ciu");
        Paginate(() => _purchaseProvider.DistinctByName()!, "Lista unikalnych obiektów wg produktów");
        Paginate(() => _purchaseProvider.ChunkProduct(5)!, "Informacje o produktach w seriach po 5 sztuk");
    }

    private void DisplayMinPurchasedProduct()
    {
        Console.Clear();
        Console.WriteLine("\n<--Najtańszy zakupiony produkt-->");
        _purchaseProvider.GetMinPurchasedProductPrice();
        Console.ReadKey();
    }

    private void DisplayMaxPurchasedProduct()
    {
        Console.Clear();
        Console.WriteLine("\n<--Najdroższy zakupiony produkt-->");
        _purchaseProvider.GetMaxPriceOfAllPurchasedProducts();
        Console.ReadKey();
    }

    private void DisplayFirstProductByPrice(decimal price)
    {
        Console.Clear();
        Console.WriteLine("\n<--Pierwszy produkt o podanej cenie-->");
        var firstPrice = _purchaseProvider.FirstOrDefaultByPriceWithDefault(price);
        Console.WriteLine($"\n- Sklep {firstPrice.NameShop} : produkt {firstPrice.Name} cena {firstPrice.Price:C}");
    }




    public void ChoiceOfMenu()
    {

        while (true)
        {


            DisplayMenu();

            var choiceValid = int.TryParse(Console.ReadLine(), out int result);

            if (choiceValid && result > 0 && result < 8)
            {
                switch (result)
                {
                    case 1:
                        _foodRepository.WriteAllToConsole();
                        Console.ReadKey();
                        break;

                    case 2:
                    case 3:
                        string productType = result == 2 ? "produkt" : "produkt bio";
                        Console.WriteLine($"Podaj nazwę {productType}");

                        var name = Console.ReadLine();

                        if (result == 2)
                            _foodRepository.Add(new Food { ProductName = name });
                        else
                            _foodRepository.Add(new BioFood { ProductName = $"{name} Bio" });

                        _foodRepository.Save();
                        break;

                    case 4:
                        _foodRepository.WriteAllToConsole();
                        Console.WriteLine("Podaj ID produktu do usunięcia");

                        if (int.TryParse(Console.ReadLine(), out int idToRemove))
                        {
                            var itemToRemove = _foodRepository.GetById(idToRemove);

                            if (itemToRemove != null)
                            {
                                _foodRepository.Remove(itemToRemove);
                                _foodRepository.Save();
                            }
                            else
                            {
                                Console.WriteLine("Produkt o podanym ID nie istnieje.");
                                Console.ReadKey();
                            }
                        }
                        break;

                    case 5:
                        ShowStatisctic();
                        Console.ReadKey();
                        break;

                    case 6:

                        var products = _csvReader.ProcessProduct("Resources\\Files\\Product.csv");
                        var purchase = _csvReader.ToProcessStatisc("Resources\\Files\\Purchase.csv");

                        GroupBy(purchase);
                        Console.WriteLine("===============Łączenie tabel=============");
                        JoinTables(products, purchase);
                        Console.ReadKey();
                        ExportToXml(purchase);

                        break;


                    case 7:
                        try
                        {
                            _foodRepository.ExportFoodListToJsonFiles();
                            Environment.Exit(0);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{e.Message}");
                            Console.ReadKey();
                        }
                        break;


                    default:
                        Alert();
                        break;
                }
            }
            else
            {
                Alert();
            }
        }
    }

    private static void JoinTables(List<Product> products, List<Purchase> purchase)
    {
        var priceProduct = products.Join(purchase,
                                    x => x.ProductName,
                                    x => x.Name,
                                    (products, purchase) => new
                                    {
                                        products.ProductName,
                                        purchase.NameShop,
                                        purchase.Price,
                                        purchase.Promotion,

                                    }).OrderBy(x => x.ProductName)
                                    .ThenBy(x => x.Price);

        foreach (var price in priceProduct)
        {
            Console.WriteLine($"{price.ProductName}");
            Console.WriteLine($"\t Price : {price.Price}");
            Console.WriteLine($"\t Promotion : {price.Promotion}");
            Console.WriteLine($"\t Shop:{price.NameShop}");

        }
    }

    private static void GroupBy(List<Purchase> purchase)
    {
        var groups = purchase.GroupBy(x => x.Name)
            .Select(g => new
            {
                Name = g.Key,
                Max = g.Max(c => c.Price),
                Min = g.Min(c => c.Price),
                Average = g.Average(c => c.Price)


            })
            .OrderBy(x => x.Name);

        foreach (var grup in groups)
        {
            Console.WriteLine($"{grup.Name}");
            Console.WriteLine($"\t Max : {grup.Max}");
            Console.WriteLine($"\t Min : {grup.Min}");
            Console.WriteLine($"\t Avg : {Math.Round(grup.Average, 2)}");
        }
    }

    private static void ExportToXml(List<Purchase> purchase)
    {
        var document = new XDocument();
        var xmlProducts = new XElement("ListaProduktow", purchase
            .Select(x =>
            new XElement("NazwaSklepu",
                new XAttribute("NazwaSklepu", x.NameShop!),
                new XAttribute("NazwaProduktu", x.Name!),
                    new XElement("Cena", x.Price),
                    new XElement("ProduktBio", x.BioFood),
                    new XElement("Promocja", x.Promotion),
            new XElement("KwotaWydanaWSklepie",
                    new XAttribute(x.NameShop!, purchase.Where(c => c.NameShop == x.NameShop).Sum(x => x.Price)),
                        new XElement("Produkty", purchase.Where(c => c.NameShop == x.NameShop)
                            .Select(x => new XElement("ListaZakupow",
                             new XAttribute("Produkt", x.Name!),
                             new XAttribute("cena", x.Price))


            ))))));

        document.Add(xmlProducts);
        document.Save("Products.xml");
    }
}
