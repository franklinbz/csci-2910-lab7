using System.ComponentModel.DataAnnotations;
using LibraryApp.Models;
using LibraryApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibraryApp.Tests;

[TestClass]
public class LibraryServiceTests
{
    private string directory = null!;

    [TestInitialize]
    public void Initialize()
    {
        directory = Path.Combine(Path.GetTempPath(), "LibraryServiceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(directory, "Data"));
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(directory, true);

    private string DataPath(string file) => Path.Combine(directory, "Data", file);

    private LibraryService CreateService() => new(new TestEnvironment { ContentRootPath = directory });

    private LibraryService SeededService()
    {
        File.WriteAllText(DataPath("Books.csv"), "1,First Book,First Author,001\n2,Second Book,Second Author,002\n");
        File.WriteAllText(DataPath("Users.csv"), "1,First User,first@example.com\n2,Second User,second@example.com\n");
        return CreateService();
    }

    private static Book NewBook() => new() { Title = "New Book", Author = "New Author", ISBN = "003" };

    private static User NewUser() => new() { Name = "New User", Email = "new@example.com" };

    private static Exception? Capture(Action action)
    {
        try { action(); return null; }
        catch (Exception error) { return error; }
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "LibraryApp";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = "";
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    [TestMethod]
    public void ReadBooks_ReturnsCsvRecords()
    {
        var service = SeededService();

        var records = service.ReadBooks();

        Assert.AreEqual(3, records.Count);
        Assert.AreEqual(1, records[0].Id);
        Assert.AreEqual("First Book", records[0].Title);
    }

    [TestMethod]
    public void ReadBooks_EmptyFile_ReturnsEmptyList()
    {
        var service = CreateService();

        var records = service.ReadBooks();

        Assert.AreEqual(0, records.Count);
    }

    [TestMethod]
    public void ReadBooks_ReturnsIndependentCopies()
    {
        var service = SeededService();

        var records = service.ReadBooks();
        records[0].Title = "Changed";

        Assert.AreEqual("First Book", service.ReadBooks()[0].Title);
    }

    [TestMethod]
    public void AddBook_AssignsFirstIdAndPersists()
    {
        var service = CreateService();
        var record = NewBook();

        service.AddBook(record);

        var saved = service.ReadBooks().Single();
        Assert.AreEqual(1, saved.Id);
        Assert.AreEqual(record.Title, saved.Title);
        Assert.AreEqual(saved.Title, CreateService().ReadBooks().Single().Title);
    }

    [TestMethod]
    public void AddBook_UsesHighestIdPlusOne()
    {
        var service = SeededService();
        service.DeleteBook(1);
        var record = NewBook();

        service.AddBook(record);

        Assert.AreEqual(3, service.ReadBooks().Last().Id);
    }

    [TestMethod]
    public void AddBook_CsvSpecialCharactersRoundTrip()
    {
        var service = CreateService();
        var record = NewBook();
        record.Title = "A, B \"C\"\nD";

        service.AddBook(record);

        Assert.AreEqual(record.Title, CreateService().ReadBooks().Single().Title);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void AddBook_InvalidTitle_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;
        record.Title = invalid;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.AddBook(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void AddBook_InvalidAuthor_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;
        record.Author = invalid;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.AddBook(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void AddBook_InvalidISBN_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;
        record.ISBN = invalid;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.AddBook(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void EditBook_InvalidTitle_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;
        record.Title = invalid;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.EditBook(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void EditBook_InvalidAuthor_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;
        record.Author = invalid;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.EditBook(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void EditBook_InvalidISBN_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;
        record.ISBN = invalid;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.EditBook(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [TestMethod]
    public void EditBook_UpdatesAllFieldsAndPersists()
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 1;

        service.EditBook(record);

        Assert.AreEqual(record.Title, service.ReadBooks().First().Title);
        Assert.AreEqual(record.Title, CreateService().ReadBooks().First().Title);
        Assert.AreEqual(record.Author, service.ReadBooks().First().Author);
        Assert.AreEqual(record.Author, CreateService().ReadBooks().First().Author);
        Assert.AreEqual(record.ISBN, service.ReadBooks().First().ISBN);
        Assert.AreEqual(record.ISBN, CreateService().ReadBooks().First().ISBN);
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [TestMethod]
    public void EditBook_MissingId_LeavesCsvUnchanged()
    {
        var service = SeededService();
        var record = NewBook();
        record.Id = 999;
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.EditBook(record));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
    }

    [TestMethod]
    public void DeleteBook_RemovesRecordAndPersists()
    {
        var service = SeededService();

        service.DeleteBook(1);

        Assert.AreEqual(2, service.ReadBooks().Single().Id);
        Assert.AreEqual(2, CreateService().ReadBooks().Single().Id);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(999)]
    public void DeleteBook_MissingId_LeavesDataUnchanged(int id)
    {
        var service = SeededService();
        var before = File.ReadAllText(DataPath("Books.csv"));

        var error = Capture(() => service.DeleteBook(id));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(2, service.ReadBooks().Count);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Books.csv")));
    }

    [TestMethod]
    public void DeleteBook_ActiveLoan_IsRejected()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        var error = Capture(() => service.DeleteBook(1));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(2, service.ReadBooks().Count);
        Assert.AreEqual(1, service.GetBorrowedBooks().Single().Value.Count);
    }

    [TestMethod]
    public void AddBook_WriteFailure_LeavesMemoryUnchanged()
    {
        var service = SeededService();
        Directory.CreateDirectory(DataPath("Books.csv.tmp"));
        var record = NewBook();
        record.Id = 1;

        var error = Capture(() => service.AddBook(record));

        Assert.IsNotNull(error);
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.AreEqual(2, service.ReadBooks().Count);
        Assert.AreEqual("First Book", service.ReadBooks().First().Title);
    }

    [TestMethod]
    public void EditBook_WriteFailure_LeavesMemoryUnchanged()
    {
        var service = SeededService();
        Directory.CreateDirectory(DataPath("Books.csv.tmp"));
        var record = NewBook();
        record.Id = 1;

        var error = Capture(() => service.EditBook(record));

        Assert.IsNotNull(error);
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.AreEqual(2, service.ReadBooks().Count);
        Assert.AreEqual("First Book", service.ReadBooks().First().Title);
    }

    [TestMethod]
    public void DeleteBook_WriteFailure_LeavesMemoryUnchanged()
    {
        var service = SeededService();
        Directory.CreateDirectory(DataPath("Books.csv.tmp"));

        var error = Capture(() => service.DeleteBook(1));

        Assert.IsNotNull(error);
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.AreEqual(2, service.ReadBooks().Count);
        Assert.AreEqual("First Book", service.ReadBooks().First().Title);
    }

    [DataTestMethod]
    [DataRow("bad,Title,Author,ISBN")]
    [DataRow("0,Title,Author,ISBN")]
    [DataRow("-1,Title,Author,ISBN")]
    [DataRow("1,Missing")]
    [DataRow("1,Title,Author,ISBN,Extra")]
    [DataRow("1,Title,Author,ISBN\n1,Other,Author,ISBN")]
    public void Constructor_MalformedBooks_IsRejected(string contents)
    {
        File.WriteAllText(DataPath("Books.csv"), contents);

        var error = Capture(() => CreateService());

        Assert.IsInstanceOfType<InvalidDataException>(error);
    }

    [TestMethod]
    public void ReadUsers_ReturnsCsvRecords()
    {
        var service = SeededService();

        var records = service.ReadUsers();

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual(1, records[0].Id);
        Assert.AreEqual("First User", records[0].Name);
    }

    [TestMethod]
    public void ReadUsers_EmptyFile_ReturnsEmptyList()
    {
        var service = CreateService();

        var records = service.ReadUsers();

        Assert.AreEqual(0, records.Count);
    }

    [TestMethod]
    public void ReadUsers_ReturnsIndependentCopies()
    {
        var service = SeededService();

        var records = service.ReadUsers();
        records[0].Name = "Changed";

        Assert.AreEqual("First User", service.ReadUsers()[0].Name);
    }

    [TestMethod]
    public void AddUser_AssignsFirstIdAndPersists()
    {
        var service = CreateService();
        var record = NewUser();

        service.AddUser(record);

        var saved = service.ReadUsers().Single();
        Assert.AreEqual(1, saved.Id);
        Assert.AreEqual(record.Name, saved.Name);
        Assert.AreEqual(saved.Name, CreateService().ReadUsers().Single().Name);
    }

    [TestMethod]
    public void AddUser_UsesHighestIdPlusOne()
    {
        var service = SeededService();
        service.DeleteUser(1);
        var record = NewUser();

        service.AddUser(record);

        Assert.AreEqual(3, service.ReadUsers().Last().Id);
    }

    [TestMethod]
    public void AddUser_CsvSpecialCharactersRoundTrip()
    {
        var service = CreateService();
        var record = NewUser();
        record.Name = "A, B \"C\"\nD";

        service.AddUser(record);

        Assert.AreEqual(record.Name, CreateService().ReadUsers().Single().Name);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void AddUser_InvalidName_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewUser();
        record.Id = 1;
        record.Name = invalid;
        var before = File.ReadAllText(DataPath("Users.csv"));

        var error = Capture(() => service.AddUser(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Users.csv")));
        Assert.AreEqual(2, service.ReadUsers().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("invalid-email")]
    public void AddUser_InvalidEmail_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewUser();
        record.Id = 1;
        record.Email = invalid;
        var before = File.ReadAllText(DataPath("Users.csv"));

        var error = Capture(() => service.AddUser(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Users.csv")));
        Assert.AreEqual(2, service.ReadUsers().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void EditUser_InvalidName_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewUser();
        record.Id = 1;
        record.Name = invalid;
        var before = File.ReadAllText(DataPath("Users.csv"));

        var error = Capture(() => service.EditUser(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Users.csv")));
        Assert.AreEqual(2, service.ReadUsers().Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("invalid-email")]
    public void EditUser_InvalidEmail_DoesNotChangeData(string invalid)
    {
        var service = SeededService();
        var record = NewUser();
        record.Id = 1;
        record.Email = invalid;
        var before = File.ReadAllText(DataPath("Users.csv"));

        var error = Capture(() => service.EditUser(record));

        Assert.IsInstanceOfType<ValidationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Users.csv")));
        Assert.AreEqual(2, service.ReadUsers().Count);
    }

    [TestMethod]
    public void EditUser_UpdatesAllFieldsAndPersists()
    {
        var service = SeededService();
        var record = NewUser();
        record.Id = 1;

        service.EditUser(record);

        Assert.AreEqual(record.Name, service.ReadUsers().First().Name);
        Assert.AreEqual(record.Name, CreateService().ReadUsers().First().Name);
        Assert.AreEqual(record.Email, service.ReadUsers().First().Email);
        Assert.AreEqual(record.Email, CreateService().ReadUsers().First().Email);
        Assert.AreEqual(2, service.ReadUsers().Count);
    }

    [TestMethod]
    public void EditUser_MissingId_LeavesCsvUnchanged()
    {
        var service = SeededService();
        var record = NewUser();
        record.Id = 999;
        var before = File.ReadAllText(DataPath("Users.csv"));

        var error = Capture(() => service.EditUser(record));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Users.csv")));
    }

    [TestMethod]
    public void DeleteUser_RemovesRecordAndPersists()
    {
        var service = SeededService();

        service.DeleteUser(1);

        Assert.AreEqual(2, service.ReadUsers().Single().Id);
        Assert.AreEqual(2, CreateService().ReadUsers().Single().Id);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(999)]
    public void DeleteUser_MissingId_LeavesDataUnchanged(int id)
    {
        var service = SeededService();
        var before = File.ReadAllText(DataPath("Users.csv"));

        var error = Capture(() => service.DeleteUser(id));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(2, service.ReadUsers().Count);
        Assert.AreEqual(before, File.ReadAllText(DataPath("Users.csv")));
    }

    [TestMethod]
    public void DeleteUser_ActiveLoan_IsRejected()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        var error = Capture(() => service.DeleteUser(1));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(2, service.ReadUsers().Count);
        Assert.AreEqual(1, service.GetBorrowedBooks().Single().Value.Count);
    }

    [TestMethod]
    public void AddUser_WriteFailure_LeavesMemoryUnchanged()
    {
        var service = SeededService();
        Directory.CreateDirectory(DataPath("Users.csv.tmp"));
        var record = NewUser();
        record.Id = 1;

        var error = Capture(() => service.AddUser(record));

        Assert.IsNotNull(error);
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.AreEqual(2, service.ReadUsers().Count);
        Assert.AreEqual("First User", service.ReadUsers().First().Name);
    }

    [TestMethod]
    public void EditUser_WriteFailure_LeavesMemoryUnchanged()
    {
        var service = SeededService();
        Directory.CreateDirectory(DataPath("Users.csv.tmp"));
        var record = NewUser();
        record.Id = 1;

        var error = Capture(() => service.EditUser(record));

        Assert.IsNotNull(error);
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.AreEqual(2, service.ReadUsers().Count);
        Assert.AreEqual("First User", service.ReadUsers().First().Name);
    }

    [TestMethod]
    public void DeleteUser_WriteFailure_LeavesMemoryUnchanged()
    {
        var service = SeededService();
        Directory.CreateDirectory(DataPath("Users.csv.tmp"));

        var error = Capture(() => service.DeleteUser(1));

        Assert.IsNotNull(error);
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.AreEqual(2, service.ReadUsers().Count);
        Assert.AreEqual("First User", service.ReadUsers().First().Name);
    }

    [DataTestMethod]
    [DataRow("bad,Name,a@example.com")]
    [DataRow("0,Name,a@example.com")]
    [DataRow("-1,Name,a@example.com")]
    [DataRow("1,Missing")]
    [DataRow("1,Name,a@example.com,Extra")]
    [DataRow("1,Name,a@example.com\n1,Other,b@example.com")]
    public void Constructor_MalformedUsers_IsRejected(string contents)
    {
        File.WriteAllText(DataPath("Users.csv"), contents);

        var error = Capture(() => CreateService());

        Assert.IsInstanceOfType<InvalidDataException>(error);
    }

    [TestMethod]
    public void Constructor_MissingFiles_CreatesEmptyFiles()
    {
        var booksPath = DataPath("Books.csv");
        var usersPath = DataPath("Users.csv");

        var service = CreateService();

        Assert.IsTrue(File.Exists(booksPath));
        Assert.IsTrue(File.Exists(usersPath));
        Assert.AreEqual(0, service.ReadBooks().Count);
        Assert.AreEqual(0, service.ReadUsers().Count);
    }

    [TestMethod]
    public void BorrowBook_AddsSelectedBookToUser()
    {
        var service = SeededService();

        service.BorrowBook(1, 1);

        var loan = service.GetBorrowedBooks().Single();
        Assert.AreEqual(1, loan.Key.Id);
        Assert.AreEqual(1, loan.Value.Single().Id);
        Assert.AreEqual(2, service.ReadBooks().Count);
    }

    [TestMethod]
    public void BorrowBook_ExistingBorrower_AddsSecondBook()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        service.BorrowBook(1, 2);

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, service.GetBorrowedBooks().Single().Value.Select(b => b.Id).ToArray());
    }

    [DataTestMethod]
    [DataRow(999, 1)]
    [DataRow(1, 999)]
    public void BorrowBook_InvalidUserOrBook_DoesNotCreateLoan(int userId, int bookId)
    {
        var service = SeededService();

        var error = Capture(() => service.BorrowBook(userId, bookId));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(0, service.GetBorrowedBooks().Count);
    }

    [TestMethod]
    public void BorrowBook_AlreadyBorrowed_IsRejected()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        var error = Capture(() => service.BorrowBook(2, 1));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(1, service.GetBorrowedBooks().Count);
        Assert.AreEqual(1, service.GetBorrowedBooks().Single().Value.Count);
    }

    [TestMethod]
    public void ReturnBooks_OneSelectedLoan_KeepsOtherLoan()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);
        service.BorrowBook(1, 2);

        service.ReturnBooks(1, new[] { 1 });

        Assert.AreEqual(2, service.GetBorrowedBooks().Single().Value.Single().Id);
    }

    [TestMethod]
    public void ReturnBooks_AllLoans_RemovesBorrowerEntry()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);
        service.BorrowBook(1, 2);

        service.ReturnBooks(1, new[] { 1, 2 });

        Assert.AreEqual(0, service.GetBorrowedBooks().Count);
    }

    [TestMethod]
    public void ReturnBooks_DuplicateSelection_ReturnsOnce()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        service.ReturnBooks(1, new[] { 1, 1 });

        Assert.AreEqual(0, service.GetBorrowedBooks().Count);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    public void ReturnBooks_InvalidSelection_PreservesAllLoans(int mode)
    {
        var service = SeededService();
        service.BorrowBook(1, 1);
        var ids = mode == 0 ? Array.Empty<int>() : new[] { 1, 999 };

        var error = Capture(() => service.ReturnBooks(1, ids));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(1, service.GetBorrowedBooks().Single().Value.Single().Id);
    }

    [TestMethod]
    public void ReturnBooks_UnknownBorrower_IsRejected()
    {
        var service = SeededService();

        var error = Capture(() => service.ReturnBooks(999, new[] { 1 }));

        Assert.IsInstanceOfType<InvalidOperationException>(error);
        Assert.AreEqual(0, service.GetBorrowedBooks().Count);
    }

    [TestMethod]
    public void EditBook_UpdatesActiveLoan()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);
        var book = NewBook();
        book.Id = 1;

        service.EditBook(book);

        Assert.AreEqual(book.Title, service.GetBorrowedBooks().Single().Value.Single().Title);
    }

    [TestMethod]
    public void EditUser_UpdatesActiveBorrower()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);
        var user = NewUser();
        user.Id = 1;

        service.EditUser(user);

        Assert.AreEqual(user.Name, service.GetBorrowedBooks().Single().Key.Name);
        Assert.AreEqual(user.Email, service.GetBorrowedBooks().Single().Key.Email);
    }

    [TestMethod]
    public void NewService_ResetsLoansAndKeepsCatalog()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        var restarted = CreateService();

        Assert.AreEqual(0, restarted.GetBorrowedBooks().Count);
        Assert.AreEqual(2, restarted.ReadBooks().Count);
    }

    [TestMethod]
    public void GetBorrowedBooks_ReturnsIndependentCopies()
    {
        var service = SeededService();
        service.BorrowBook(1, 1);

        var loans = service.GetBorrowedBooks();
        loans.Single().Key.Name = "Changed";
        loans.Single().Value.Clear();

        Assert.AreEqual("First User", service.GetBorrowedBooks().Single().Key.Name);
        Assert.AreEqual(1, service.GetBorrowedBooks().Single().Value.Count);
    }
}
