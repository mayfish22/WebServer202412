using WebServer.Models.WebServerDB;

namespace WebServer.Models.ViewModels;

public class OrderPreviewViewModel
{
    public required Order Order { get; set; }
    public required List<OrderDetail> OrderDetails { get; set; }
}