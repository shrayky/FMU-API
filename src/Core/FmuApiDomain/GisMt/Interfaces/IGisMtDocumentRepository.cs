using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Хранение загруженных входящих документов ГИС МТ: факт загрузки для исключения повторов и список для просмотра.
/// </summary>
public interface IGisMtDocumentRepository
{
    Task<GisMtDocumentEntity?> Get(string id);

    Task<bool> Exists(string id);

    Task<bool> Save(GisMtDocumentEntity entity);

    /// <summary>
    /// Страница документов по убыванию даты загрузки, без тела ответа doc/info.
    /// </summary>
    Task<Result<GisMtDocumentPage>> List(int page, int pageSize);
}
