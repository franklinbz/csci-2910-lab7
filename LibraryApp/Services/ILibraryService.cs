using LibraryApp.Models;
namespace LibraryApp.Services;
public interface ILibraryService
{
    IReadOnlyList<Book> ReadBooks();
    IReadOnlyList<User> ReadUsers();
    void AddBook(Book book);
    void EditBook(Book book);
    void DeleteBook(int id);
    void AddUser(User user);
    void EditUser(User user);
    void DeleteUser(int id);
    IReadOnlyDictionary<User, List<Book>> GetBorrowedBooks();
    void BorrowBook(int userId, int bookId);
    void ReturnBooks(int userId, IEnumerable<int> bookIds);
}
