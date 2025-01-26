using AppShoping.Data.Entities;

namespace AppShoping.Components.DataProviders.Extensions
{
    public static class PurchaseExtensions
    {
        public static IEnumerable<PurchaseStatistics> OrderByNameAndPrice(IEnumerable<PurchaseStatistics> query)
        {

            return query.OrderBy(x => x.Price)
                        .ThenBy(x => x.Name);

        }


    }
}
