using AppShoping.Components.csvReader.Models;

namespace AppShoping.Components.csvReader
{
    public interface ICsvReader
    {
        List<Product> ProcessProduct(string filePath);
        List<Purchase> ToProcessStatisc (string filePath);

    }
}
