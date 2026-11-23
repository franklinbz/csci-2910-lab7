using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using LibraryApp.Models;
namespace LibraryApp.Services;

public class LibraryService : ILibraryService
{
    private readonly object gate = new();
    private readonly string booksPath, usersPath;
    private List<Book> books = new();
    private List<User> users = new();
    private readonly Dictionary<User, List<Book>> borrowedBooks = new();

    public LibraryService(IWebHostEnvironment environment)
    {
        var data = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(data);
        booksPath = Path.Combine(data, "Books.csv");
        usersPath = Path.Combine(data, "Users.csv");
        books = ReadRows(booksPath, 4).Select(f => new Book { Id = int.Parse(f[0]), Title = f[1], Author = f[2], ISBN = f[3] }).ToList();
        users = ReadRows(usersPath, 3).Select(f => new User { Id = int.Parse(f[0]), Name = f[1], Email = f[2] }).ToList();
    }
    private static List<string[]> ReadRows(string path, int count)
    {
        if (!File.Exists(path)) { File.WriteAllText(path, ""); return new(); }
        using var parser = new TextFieldParser(path, System.Text.Encoding.UTF8);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        var rows = new List<string[]>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length != count || !int.TryParse(fields[0], out var id) || id < 1)
                throw new InvalidDataException($"Invalid CSV record in {Path.GetFileName(path)}.");
            rows.Add(fields);
        }
        if (rows.Select(r => r[0]).Distinct().Count() != rows.Count) throw new InvalidDataException("Duplicate record IDs.");
        return rows;
    }
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static void WriteRows(string path, IEnumerable<string[]> rows)
    {
        var temporary = path + ".tmp";
        File.WriteAllLines(temporary, rows.Select(r => string.Join(",", r.Select(Csv))), new System.Text.UTF8Encoding(false));
        File.Move(temporary, path, true);
    }
    private void SaveBooks(List<Book> next) => WriteRows(booksPath, next.Select(b => new[] { b.Id.ToString(CultureInfo.InvariantCulture), b.Title, b.Author, b.ISBN }));
    private void SaveUsers(List<User> next) => WriteRows(usersPath, next.Select(u => new[] { u.Id.ToString(CultureInfo.InvariantCulture), u.Name, u.Email }));
    private static Book Copy(Book b) => new() { Id = b.Id, Title = b.Title, Author = b.Author, ISBN = b.ISBN };
    private static User Copy(User u) => new() { Id = u.Id, Name = u.Name, Email = u.Email };
    private static void Validate(object model) => Validator.ValidateObject(model, new ValidationContext(model), true);
    public IReadOnlyList<Book> ReadBooks() { lock (gate) return books.Select(Copy).ToList(); }
    public IReadOnlyList<User> ReadUsers() { lock (gate) return users.Select(Copy).ToList(); }
    public IReadOnlyDictionary<User, List<Book>> GetBorrowedBooks()
    {
        lock (gate) return borrowedBooks.ToDictionary(p => Copy(p.Key), p => p.Value.Select(Copy).ToList());
    }
    public void AddBook(Book book)
    {
        lock (gate) { Validate(book); var added = Copy(book); added.Id = books.Select(b => b.Id).DefaultIfEmpty().Max() + 1;
            var next = books.Append(added).ToList(); SaveBooks(next); books = next; }
    }
    public void EditBook(Book book)
    {
        lock (gate) { Validate(book); var existing = books.SingleOrDefault(b => b.Id == book.Id) ?? throw new InvalidOperationException("Book not found.");
            var next = books.Select(b => b.Id == book.Id ? Copy(book) : b).ToList(); SaveBooks(next); books = next;
            existing.Title = book.Title; existing.Author = book.Author; existing.ISBN = book.ISBN;
            foreach (var loan in borrowedBooks.Values.SelectMany(x => x).Where(b => b.Id == book.Id)) { loan.Title = book.Title; loan.Author = book.Author; loan.ISBN = book.ISBN; } }
    }
    public void DeleteBook(int id)
    {
        lock (gate) { if (borrowedBooks.Values.Any(list => list.Any(b => b.Id == id))) throw new InvalidOperationException("Return this book before deleting it.");
            if (!books.Any(b => b.Id == id)) throw new InvalidOperationException("Book not found.");
            var next = books.Where(b => b.Id != id).ToList(); SaveBooks(next); books = next; }
    }
    public void AddUser(User user)
    {
        lock (gate) { Validate(user); var added = Copy(user); added.Id = users.Select(u => u.Id).DefaultIfEmpty().Max() + 1;
            var next = users.Append(added).ToList(); SaveUsers(next); users = next; }
    }
    public void EditUser(User user)
    {
        lock (gate) { Validate(user); var existing = users.SingleOrDefault(u => u.Id == user.Id) ?? throw new InvalidOperationException("User not found.");
            var next = users.Select(u => u.Id == user.Id ? Copy(user) : u).ToList(); SaveUsers(next); users = next;
            var key = borrowedBooks.Keys.SingleOrDefault(u => u.Id == user.Id);
            if (key is not null) { key.Name = user.Name; key.Email = user.Email; } }
    }
    public void DeleteUser(int id)
    {
        lock (gate) { if (borrowedBooks.Any(p => p.Key.Id == id && p.Value.Count > 0)) throw new InvalidOperationException("Return this user's books before deleting the user.");
            if (!users.Any(u => u.Id == id)) throw new InvalidOperationException("User not found.");
            var next = users.Where(u => u.Id != id).ToList(); SaveUsers(next); users = next; }
    }
    public void BorrowBook(int userId, int bookId)
    {
        lock (gate) { var user = users.SingleOrDefault(u => u.Id == userId) ?? throw new InvalidOperationException("Select a valid user.");
            var book = books.SingleOrDefault(b => b.Id == bookId) ?? throw new InvalidOperationException("Select a valid book.");
            if (borrowedBooks.Values.Any(list => list.Any(b => b.Id == bookId))) throw new InvalidOperationException("This book is already borrowed.");
            var key = borrowedBooks.Keys.SingleOrDefault(u => u.Id == userId) ?? user;
            if (!borrowedBooks.ContainsKey(key)) borrowedBooks[key] = new();
            borrowedBooks[key].Add(book); }
    }
    public void ReturnBooks(int userId, IEnumerable<int> bookIds)
    {
        lock (gate) { var key = borrowedBooks.Keys.SingleOrDefault(u => u.Id == userId) ?? throw new InvalidOperationException("This user has no borrowed books.");
            var ids = bookIds.Distinct().ToList();
            if (ids.Count == 0 || ids.Any(id => !borrowedBooks[key].Any(b => b.Id == id))) throw new InvalidOperationException("Select borrowed books to return.");
            borrowedBooks[key].RemoveAll(b => ids.Contains(b.Id));
            if (borrowedBooks[key].Count == 0) borrowedBooks.Remove(key); }
    }
}
