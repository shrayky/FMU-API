using CouchDb.DatabaseScheme;
using CouchDb.Documents;
using CouchDB.Driver;
using FmuApiDomain.BeerTaps.Entities;
using FmuApiDomain.Documents.Entities;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.Mark.Entities;
using FmuApiDomain.ProductGroups.Entities;
using FmuApiDomain.Statistics.Entities;

namespace CouchDb;

public class CouchDbContext
{
    public ICouchDatabase<CouchDoc<MarkEntity>> Marks { get; }
    public ICouchDatabase<CouchDoc<DocumentEntity>> Documents { get; }
    public ICouchDatabase<CouchDoc<StatisticEntity>> MarkCheckingStatistic { get; }
    public ICouchDatabase<CouchDoc<BeerTapEntity>> BeerOnTap { get; }
    public ICouchDatabase<CouchDoc<GisMtDocumentEntity>> GisMtDocuments { get; }
    public ICouchDatabase<CouchDoc<GisMtMarkEntity>> GisMtMarks { get; }
    public ICouchDatabase<CouchDoc<GtinCatalogEntity>> GtinCatalog { get; }

    public CouchDbContext(CouchClient client)
    {
        Marks = client.GetDatabase<CouchDoc<MarkEntity>>(DatabaseNames.MarksDbName);
        Documents = client.GetDatabase<CouchDoc<DocumentEntity>>(DatabaseNames.DocumentsDbName);
        MarkCheckingStatistic = client.GetDatabase<CouchDoc<StatisticEntity>>(DatabaseNames.MarkCheckingStatistic);
        BeerOnTap = client.GetDatabase<CouchDoc<BeerTapEntity>>(DatabaseNames.BeerOnTaps);
        GisMtDocuments = client.GetDatabase<CouchDoc<GisMtDocumentEntity>>(DatabaseNames.GisMtDocumentsDbName);
        GisMtMarks = client.GetDatabase<CouchDoc<GisMtMarkEntity>>(DatabaseNames.GisMtMarksDbName);
        GtinCatalog = client.GetDatabase<CouchDoc<GtinCatalogEntity>>(DatabaseNames.GtinCatalogDbName);
    }
}