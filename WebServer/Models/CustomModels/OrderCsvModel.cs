using CsvHelper.Configuration.Attributes;

namespace WebServer.Models.CustomModels;

public class OrderCsvModel
{
    [Name("訂單日期")]
    [NameIndex(0)]
    public string OrderDate { get; set; }

    [Name("訂單序號")]
    [NameIndex(1)]
    public string OrderSeq { get; set; }

    [Name("訂單編號")]
    [NameIndex(2)]
    public string OrderNo { get; set; }

    [Name("金額")]
    [NameIndex(3)]
    public string TotalAmount { get; set; }

    [Name("付款狀態")]
    [NameIndex(4)]
    public string PaymentStatus { get; set; }

    [Name("備註")]
    [NameIndex(5)]
    public string Remark { get; set; }
}