// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)
//
//Copyright (C) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SampleSupport;
using Task.Data;

using Task.Data.HwLinq;

// Version Mad01

namespace SampleQueries
{
    [Title("LINQ Module")]
    [Prefix("Linq")]
    public class LinqSamples : SampleHarness
    {
        private DataSource dataSource = new DataSource();

        [Category("Homework")]
        [Title("Task01")]
        [Description("1. Выдайте список всех клиентов, чей суммарный оборот (сумма всех заказов) превосходит некоторую величину X. Продемонстрируйте выполнение запроса с различными X (подумайте, можно ли обойтись без копирования запроса несколько раз)")]
        public void Linq01()
        {
            Func<decimal, IEnumerable<Customer>> query = (x) => dataSource.Customers.Where(c => c.Orders.Sum(o => o.Total) > x);

            decimal X = 100000;
            var customers1 = query(X);

            X = 10000;
            var customers2 = query(X);

            this.DumpIEnumerable(customers1);
            this.DumpIEnumerable(customers2);
        }

        [Category("Homework")]
        [Title("Task02")]
        [Description("2. Для каждого клиента составьте список поставщиков, находящихся в той же стране и том же городе. Сделайте задания с использованием группировки и без.")]
        public void Linq02()
        {
			//Мой вариант. Без группировки
			IEnumerable mySupplierForCustomer = dataSource.Customers.Select(c => new
			{
				CompanyName = c.CompanyName,
				Country = c.Country,
				City = c.City,
				Suppliers = dataSource.Suppliers.Where(s => s.Country == c.Country && s.City == c.City)
			}).OrderBy(c => c.Country).ThenBy(c => c.Country);

            var suppliersForCustomer1 = dataSource.Customers.GroupJoin(
                dataSource.Suppliers,
                (Customer cust) => new { cust.Country, cust.City },
                (Supplier suppl) => new { suppl.Country, suppl.City },
                (customer, suppliers) => new
                {
                    customer.CompanyName,
                    customer.Country,
                    customer.City,
                    Suppliers = suppliers
                });

            /*
             * The second variant (with grouping)
             */
            var groupedSuppliers = dataSource.Suppliers
                .GroupBy(suppl => new { suppl.Country, suppl.City })
                .Select(group => new
                {
                    Place = group.Key,
                    Suppliers = group.ToList()
                });

            var groupedCustomers = dataSource.Customers
                .GroupBy(suppl => new { suppl.Country, suppl.City })
                .Select(group => new
                {
                    Place = group.Key,
                    Customers = group.ToList()
                });

            var suppliersForCustomer2 = groupedCustomers.Join(
                groupedSuppliers,
                cust => cust.Place,
                suppl => suppl.Place == null ? null : suppl.Place ,
                (customers, suppliers) => customers.Customers.Select(cust => new
                {
                    cust.CompanyName,
                    cust.Country,
                    cust.City,
                    Suppliers = suppliers.Suppliers
                }))
                .SelectMany(customer => customer);

			this.DumpIEnumerable(mySupplierForCustomer, 1);
			this.DumpIEnumerable(suppliersForCustomer1, 1);
			this.DumpIEnumerable(suppliersForCustomer2, 1);
		}

    [Category("Homework")]
    [Title("Task03")]
    [Description("3. Найдите всех клиентов, у которых были заказы, превосходящие по сумме величину X")]
    public void Linq03()
    {
        var customers = dataSource.Customers
            .Where(c => c.Orders.Any(o => o.Total > 10000));

        this.DumpIEnumerable(customers);
    }

    [Category("Homework")]
    [Title("Task04")]
    [Description("4. Выдайте список клиентов с указанием, начиная с какого месяца какого года они стали клиентами (принять за таковые месяц и год самого первого заказа)")]
    public void Linq04()
    {
		//Это будет работать, если у нас коллекция Orders уже отсортирована по дате. Для надёжности надо брать Min вместо First
        var customers = dataSource.Customers
            .Select(c => new { Customer = c.CompanyName, CustomerFrom = c.Orders.First().OrderDate.ToString("MM.yyyy") });

        this.DumpIEnumerable(customers);
    }

    [Category("Homework")]
    [Title("Task05")]
    [Description("5. Сделайте предыдущее задание, но выдайте список отсортированным по году, месяцу, оборотам клиента (от максимального к минимальному) и имени клиента")]
    public void Linq05()
    {
			//то же замечание, что и в предидущем примере. Плюс, если мы сортируем по оборотам, то надо их тоже выводить. Иначе для клиента это будет не очевидно,
			//и он начнёт нервничать
			//Мой вариант
			var myCustomers = dataSource.Customers
				.Where(c => c.Orders.Any())
				.OrderBy(c => c.Orders.Min(x => x.OrderDate).Year)
				.ThenBy(c => c.Orders.Min(x => x.OrderDate).Month)
				.ThenByDescending(c => c.Orders.Sum(o => o.Total))
				.ThenBy(c => c.CompanyName)
				.Select(c => new { Customer = c.CompanyName, CustomerFrom = c.Orders.First().OrderDate.ToString("MM.yyyy"), Total = c.Orders.Sum(o => o.Total) });

			var customers = dataSource.Customers
            .Where(c => c.Orders.Any())
            .OrderBy(c => c.Orders.First().OrderDate.Year)
            .ThenBy(c => c.Orders.First().OrderDate.Month)
            .ThenByDescending(c => c.Orders.Sum(o => o.Total))
            .ThenBy(c => c.CompanyName)
            .Select(c => new { Customer = c.CompanyName, CustomerFrom = c.Orders.First().OrderDate.ToString("MM.yyyy") });

        this.DumpIEnumerable(myCustomers);
    }

    [Category("Homework")]
    [Title("Task06")]
    [Description("6. Укажите всех клиентов, у которых указан нецифровой почтовый код или не заполнен регион или в телефоне не указан код оператора (для простоты считаем, что это равнозначно «нет круглых скобочек в начале»).")]
    public void Linq06()
    {
        int number;

        var customers = dataSource.Customers
            .Where(c => int.TryParse(c.PostalCode, out number) == false || string.IsNullOrEmpty(c.Region) || c.Phone.StartsWith("(") == false);

        this.DumpIEnumerable(customers);
    }

    [Category("Homework")]
    [Title("Task07")]
    [Description("7. Сгруппируйте все продукты по категориям, внутри – по наличию на складе, внутри последней группы отсортируйте по стоимости")]
    public void Linq07()
    {
        var products = dataSource.Products
            .GroupBy(p => p.Category).Select(categoryGroup => new
            {
                Category = categoryGroup.Key,
                StockGroups = categoryGroup.GroupBy(p => p.UnitsInStock > 0).Select(stockGroup => new
                {
                    InStock = stockGroup.Key,
                    PriceGroups = stockGroup.GroupBy(p => p.UnitPrice).Select(priceGroup => new
                    {
                        Price = priceGroup.Key,
                        Products = priceGroup
                    })
                })
            });



        /*
         * The second variant 
         */
        var products1 =
            from prod in dataSource.Products
            group prod by prod.Category into categoryGroup
            select new
            {
                Category = categoryGroup.Key,
                StockGroups = from prod in categoryGroup
                              group prod by prod.UnitsInStock > 0 into stockGroup
                              select new
                              {
                                  InStock = stockGroup.Key,
                                  PriceGroups = from prod in dataSource.Products
                                                group prod by prod.UnitPrice into priceGroup
                                                select new { Price = priceGroup.Key, Products = priceGroup }
                              }
            };

        this.DumpIEnumerable(products);
    }

    [Category("Homework")]
    [Title("Task08")]
    [Description("8. Сгруппируйте товары по группам «дешевые», «средняя цена», «дорогие». Границы каждой группы задайте сами")]
    public void Linq08()
    {
        decimal cheapLimit = 20;
        decimal mediumLimit = 45;

        var products = dataSource.Products
            .Select(p => new
            {
                Product = p,
                PriceCategory = p.UnitPrice < cheapLimit ? PriceCategory.Cheap : (p.UnitPrice >= cheapLimit && p.UnitPrice < mediumLimit ? PriceCategory.Medium : PriceCategory.Expensive)
            })
            .GroupBy(p => p.PriceCategory)
            .Select(productsGroup => new
            {
                PriceCategory = productsGroup.Key,
                Products = productsGroup.Select(productAndCategory => productAndCategory.Product)
            })
            .OrderBy(productGroup => productGroup.PriceCategory);

        this.DumpIEnumerable(products, 1);
    }

    [Category("Homework")]
    [Title("Task09")]
    [Description("9. Рассчитайте среднюю прибыльность каждого города (среднюю сумму заказа по всем клиентам из данного города) и среднюю интенсивность (среднее количество заказов, приходящееся на клиента из каждого города)")]
    public void Linq09()
    {
        var cityGroups = dataSource.Customers.GroupBy(customer => customer.City);

        var citiesProfitability = cityGroups
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                Profitability = cityGroup.SelectMany(customer => customer.Orders).Average(order => order.Total)
            });

        var citiesIntensity = cityGroups
            .Select(cityGroup => new
            {
                City = cityGroup.Key,
                Intensity = cityGroup.Select(customer => customer.Orders.Count()).Average()
                //Intensity = cityGroup.SelectMany(customer => customer.Orders).Count() / (double)cityGroup.Count()
            });

        this.DumpIEnumerable(citiesProfitability);
        this.DumpIEnumerable(citiesIntensity);
    }

    [Category("Homework")]
    [Title("Task10")]
    [Description("10. Сделайте среднегодовую статистику активности клиентов по месяцам (без учета года), статистику по годам, по годам и месяцам (т.е. когда один месяц в разные годы имеет своё значение)")]
    public void Linq10()
    {
        var activity = dataSource.Customers
            .Select(customer => new
            {
                Customer = customer.CompanyName,

                MonthOnlyActivity = customer.Orders.GroupBy(order => order.OrderDate.Month).Select(monthOnlyGroup => new
                {
                    Month = monthOnlyGroup.Key,
                    NumberOfOrders = monthOnlyGroup.Count()
                }).OrderBy(monthOnlyGroup => monthOnlyGroup.Month),

                YearsAndMonthActivity = customer.Orders.GroupBy(order => order.OrderDate.Year).Select(yearGroup => new
                {
                    Year = yearGroup.Key,
                    MonthsGroups = yearGroup.GroupBy(order => order.OrderDate.Month).Select(monthInYearGroup => new
                    {
                        Month = monthInYearGroup.Key,
                        Activity = monthInYearGroup.Count()
                    }).OrderBy(monthInYearGroup => monthInYearGroup.Month)
                }).OrderBy(yearsGroup => yearsGroup.Year)
            });

        this.DumpIEnumerable(activity);
    }

    private void DumpIEnumerable(IEnumerable list, int depth = 0)
    {
        foreach (var item in list)
        {
            ObjectDumper.Write(item, depth);
        }
    }
}
}
