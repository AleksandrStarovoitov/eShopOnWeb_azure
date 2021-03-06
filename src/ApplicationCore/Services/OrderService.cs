using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly ILogger<OrderService> _logger;
    private readonly IConfiguration _configuration;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        ILogger<OrderService> logger,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);   
        await _orderRepository.AddAsync(order);

        await SendOrderToProcessor(order);
        await SendOrderMessageToReserver(order);
    }

    private async Task SendOrderToProcessor(Order order)
    {
        var httpClient = new HttpClient();
        var content = JsonContent.Create(order);
        await httpClient.PostAsync(_configuration["DeliveryOrderProcessorUrl"], content);

        _logger.LogInformation("Sent to DeliveryOrderProcessor: {0}", content.ToString());
    }

    private async Task SendOrderMessageToReserver(Order order)
    {
        var orderMessages = order.OrderItems.Select(x => new { ItemId = x.Id, Count = x.Units });

        await using var client = new ServiceBusClient(_configuration["ServiceBusConnectionString"]);
        await using var sender = client.CreateSender("eshop-queue");

        var messageContents = orderMessages.Select(x => JsonSerializer.Serialize(x));
        var messages = messageContents.Select(x => new ServiceBusMessage(x));
        await sender.SendMessagesAsync(messages);

        _logger.LogInformation("Sent to service bus: {0}", String.Join(',', messageContents));
    }
}
