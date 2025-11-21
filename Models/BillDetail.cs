namespace SelfCheckoutKiosk.Models;

public class BillDetail
{
    public int BillDtlRowId { get; set; }
    public string ProdBillDesc { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public int ProdKey { get; set; }
    public int ModifierActionKey { get; set; }
    public int ModifierDescKey { get; set; }
    public decimal ItemDiscAmt { get; set; }
    public decimal BillWiseDiscAmt { get; set; }
    public DateTime EntryDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SendDateTime { get; set; }
    public int IsVoid { get; set; }
    public int IsModifier { get; set; }
    public int ItemIDModifier { get; set; }
    public int DiscReasonKey { get; set; }
    public int VoidReasonKey { get; set; }
    public int ProdUomKey { get; set; }
    public int AuthorizedBy { get; set; }
    public string? CourseDesc { get; set; }
    public string? OrderedBy { get; set; }
    public decimal VatAmt { get; set; }
    public decimal Vat { get; set; }
    public int SeqNo { get; set; }
}
