﻿using FmuApiDomain.Configuration.Options;

namespace CouchDb
{
    public static class DatabaseNames
    {
        public const string MarksDbName = "fmu-api-marks";
        public const string DocumentsDbName = "fmu-api-documents";
        public static string[] Names() => [MarksDbName, DocumentsDbName];
        // устаревшие:
        public static string MarksStateDb { get; private set; } = string.Empty;
        public static string FrontolDocumentsDb { get; private set; } = string.Empty;
        public static string AlcoStampsDb { get; private set; } = string.Empty;

        public static void Initialize(CouchDbConnection settings)
        {
            if (!string.IsNullOrEmpty(settings.MarksStateDbName))
                MarksStateDb = settings.MarksStateDbName.ToLower();

            if (!string.IsNullOrEmpty(settings.FrontolDocumentsDbName))
                FrontolDocumentsDb = settings.FrontolDocumentsDbName.ToLower();
                
            if (!string.IsNullOrEmpty(settings.AlcoStampsDbName))
                AlcoStampsDb = settings.AlcoStampsDbName.ToLower();
        }
    }
}
