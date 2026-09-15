namespace Manager.BusinessLogic.Models;

// 
// Модель для представления результата постраничного запроса, содержащая элементы текущей страницы, общее количество элементов, номер страницы и размер страницы.
// 
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}