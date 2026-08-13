using CouchDb.DatabaseScheme;
using CouchDb.Documents;
using CouchDB.Driver;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.MarkInformation.Entities;

namespace CouchDb;

public class CouchDbContext
{
    public ICouchDatabase<CouchDoc<MarkEntity>> Marks { get; }
    public ICouchDatabase<CouchDoc<DocumentEntity>> Documents { get; }
    public ICouchDatabase<CouchDoc<StatisticEntity>> MarkCheckingStatistic { get; }
    public ICouchDatabase<CouchDoc<BeerTapEntity>> BeerOnTap { get; }
    public ICouchDatabase<CouchDoc<GisMtDocumentEntity>> GisMtDocuments { get; }
    public ICouchDatabase<CouchDoc<GisMtMarkEntity>> GisMtMarks { get; }

    public CouchDbContext(CouchClient client)
    {
        Marks = client.GetDatabase<CouchDoc<MarkEntity>>(DatabaseNames.MarksDbName);
        Documents = client.GetDatabase<CouchDoc<DocumentEntity>>(DatabaseNames.DocumentsDbName);
        MarkCheckingStatistic = client.GetDatabase<CouchDoc<StatisticEntity>>(DatabaseNames.MarkCheckingStatistic);
        BeerOnTap = client.GetDatabase<CouchDoc<BeerTapEntity>>(DatabaseNames.BeerOnTaps);
        GisMtDocuments = client.GetDatabase<CouchDoc<GisMtDocumentEntity>>(DatabaseNames.GisMtDocumentsDbName);
        GisMtMarks = client.GetDatabase<CouchDoc<GisMtMarkEntity>>(DatabaseNames.GisMtMarksDbName);
    }
}