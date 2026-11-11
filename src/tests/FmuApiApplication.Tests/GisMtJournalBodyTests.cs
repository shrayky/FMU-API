using FmuApiApplication.GisMt.Journal;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Journal;
using System.Text.Json.Nodes;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtJournalBodyTests
{
    private static GisMtJournalEntity ConnectTapDocument() => new()
    {
        Kind = GisMtJournalKinds.ConnectTap,
        Inn = "2464122182",
        DocDate = new DateOnly(2026, 11, 2),
        Lines = [new GisMtJournalLine { Cis = "0104603334003509215ABC", ConnectDate = new DateOnly(2026, 11, 2) }]
    };

    [Fact]
    public void Юридическое_лицо_передаёт_КПП_и_дату_подключения()
    {
        var participant = GisMtParticipantRules.Resolve("2464122182", "246401001", "").Value;

        var body = JsonNode.Parse(GisMtConnectTapBody.Build(ConnectTapDocument(), participant))!.AsObject();

        Assert.Equal("2464122182", body["participantInn"]!.GetValue<string>());
        Assert.Equal("246401001", body["participantKpp"]!.GetValue<string>());
        Assert.False(body.ContainsKey("fiasId"));

        var code = body["codes"]!.AsArray()[0]!.AsObject();
        Assert.Equal("0104603334003509215ABC", code["cis"]!.GetValue<string>());
        Assert.Equal("2026-11-02", code["connectDate"]!.GetValue<string>());
    }

    [Fact]
    public void ИП_передаёт_ФИАС_без_КПП()
    {
        var participant = GisMtParticipantRules.Resolve("246412218294", "", "fias-1").Value;

        var body = JsonNode.Parse(GisMtConnectTapBody.Build(ConnectTapDocument(), participant))!.AsObject();

        Assert.Equal("fias-1", body["fiasId"]!.GetValue<string>());
        Assert.False(body.ContainsKey("participantKpp"));
    }

    [Fact]
    public void Списание_передаёт_только_суммы_которые_есть()
    {
        var document = new GisMtJournalEntity
        {
            Kind = GisMtJournalKinds.LkReceipt,
            Inn = "2464122182",
            DocDate = new DateOnly(2026, 11, 2),
            Lines =
            [
                new GisMtJournalLine { Cis = "0104603334003509215A", AmountKop = 12500 },
                new GisMtJournalLine { Cis = "0104603334003509215B", AmountKop = null }
            ]
        };

        var participant = GisMtParticipantRules.Resolve("2464122182", "246401001", "").Value;

        var body = JsonNode.Parse(GisMtLkReceiptBody.Build(document, participant))!.AsObject();

        Assert.Equal("2464122182", body["inn"]!.GetValue<string>());
        Assert.Equal("PACKING", body["action"]!.GetValue<string>());
        Assert.Equal("2026-11-02", body["action_date"]!.GetValue<string>());
        Assert.Equal("246401001", body["kpp"]!.GetValue<string>());
        Assert.False(body.ContainsKey("fias_id"));
        Assert.False(body.ContainsKey("document_type"));

        var products = body["products"]!.AsArray();
        Assert.Equal(12500, products[0]!.AsObject()["product_cost"]!.GetValue<int>());
        Assert.False(products[1]!.AsObject().ContainsKey("product_cost"));
    }

    [Fact]
    public void Списание_для_ИП_передаёт_ФИАС()
    {
        var document = new GisMtJournalEntity
        {
            Kind = GisMtJournalKinds.LkReceipt,
            Inn = "246412218294",
            DocDate = new DateOnly(2026, 11, 2),
            Lines = [new GisMtJournalLine { Cis = "0104603334003509215A", AmountKop = 100 }]
        };

        var participant = GisMtParticipantRules.Resolve("246412218294", "", "fias-1").Value;

        var body = JsonNode.Parse(GisMtLkReceiptBody.Build(document, participant))!.AsObject();

        Assert.Equal("fias-1", body["fias_id"]!.GetValue<string>());
        Assert.False(body.ContainsKey("kpp"));
    }

    [Fact]
    public void Списание_по_сроку_годности_передаёт_причину_и_акт_списания()
    {
        var document = new GisMtJournalEntity
        {
            Id = "9abd3d41-1111-2222-3333-444455556666",
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = GisMtWriteOffReasons.Expiration,
            Inn = "2464122182",
            DocDate = new DateOnly(2026, 11, 10),
            Lines = [new GisMtJournalLine { Cis = "0104603334003509215A", AmountKop = null }]
        };

        var participant = GisMtParticipantRules
            .Resolve("2464122182", "246401001", "", GisMtModRequirement.Required(document.Kind, document.Reason))
            .Value;

        var body = JsonNode.Parse(GisMtLkReceiptBody.Build(document, participant))!.AsObject();

        Assert.Equal("EXPIRATION", body["action"]!.GetValue<string>());
        Assert.Equal("OTHER", body["document_type"]!.GetValue<string>());
        Assert.Equal("2026-11-10-9abd3d41", body["document_number"]!.GetValue<string>());
        Assert.Equal("2026-11-10", body["document_date"]!.GetValue<string>());
        Assert.Equal("Акт списания марок по истечении срока годности", body["primary_document_custom_name"]!.GetValue<string>());
        Assert.False(body.ContainsKey("kpp"));
        Assert.False(body.ContainsKey("fias_id"));
        Assert.False(body["products"]!.AsArray()[0]!.AsObject().ContainsKey("product_cost"));
    }

    [Fact]
    public void Списание_по_сроку_годности_ИП_без_ФИАС_уходит_без_МОД()
    {
        var document = new GisMtJournalEntity
        {
            Id = "11112222-3333-4444-5555-666677778888",
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = GisMtWriteOffReasons.Expiration,
            Inn = "246412218294",
            DocDate = new DateOnly(2026, 11, 10),
            Lines = [new GisMtJournalLine { Cis = "0104603334003509215A" }]
        };

        var participant = GisMtParticipantRules
            .Resolve("246412218294", "", "", GisMtModRequirement.Required(document.Kind, document.Reason))
            .Value;

        var body = JsonNode.Parse(GisMtLkReceiptBody.Build(document, participant))!.AsObject();

        Assert.Equal("2026-11-10-11112222", body["document_number"]!.GetValue<string>());
        Assert.False(body.ContainsKey("kpp"));
        Assert.False(body.ContainsKey("fias_id"));
    }

    [Fact]
    public void Акт_без_причины_выбытия_считается_фасовкой()
    {
        var document = new GisMtJournalEntity
        {
            Kind = GisMtJournalKinds.LkReceipt,
            Reason = string.Empty,
            Inn = "2464122182",
            DocDate = new DateOnly(2026, 11, 10),
            Lines = [new GisMtJournalLine { Cis = "0104603334003509215A" }]
        };

        var participant = GisMtParticipantRules.Resolve("2464122182", "246401001", "").Value;

        var body = JsonNode.Parse(GisMtLkReceiptBody.Build(document, participant))!.AsObject();

        Assert.Equal(GisMtWriteOffReasons.Packing, body["action"]!.GetValue<string>());
    }
}
