using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
List<string> Notifications = new List<string>();
Repository repository = new Repository();
List<Order> Orders = new List<Order>
{
    new Order("Холодильник","3d12345AF","Не охлаждает должным образом","Волегжанин Кирилл Алексеевич","+79095682349")

};
repository.Orders = Orders;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Open", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
var app = builder.Build();
app.UseCors("Open");
app.Use(async (context, next) => 
{
    try
    {
        await next.Invoke();
    }
    catch(ArgumentException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(ex.Message));
    }

});
app.MapGet("/", () =>
{
    var data = new
    {
        orders = repository.ReadAll(),
        notifications = Notifications.ToList(),
    };
    Notifications.Clear();
    return Results.Json(data);
});
app.MapGet("/order/id/{id}", (int id) =>
{
    var data = new
    {
        orders = new[] { repository.Read(id) }
    };
    return Results.Json(data);
});
app.MapGet("/statistics", () =>
{
    var statistics = new
    {
        CompletedOrders = repository.CompleteOrders(),
        AverageExecutionTime = repository.AverageExecutionTime(),
        ProblemTypeStatistics = repository.ProblemTypeStatictics()
    };
    return Results.Json(statistics);
});
app.MapPost("/order/add", (Order order) =>
{
    repository.AddOrder(new Order(order.View, order.Model, order.Description, order.FullName, order.Phone));
    Notifications.Add("Order added");
    return Results.Ok("Order added");
});
app.MapPut("/order/update/{id}", (int id, Order order) =>
{
    var orderOld = repository.Read(id);
    orderOld.View = order.View;
    orderOld.Model = order.Model;
    orderOld.Description = order.Description;
    orderOld.FullName = order.FullName;
    orderOld.Phone = order.Phone;
    orderOld.Master = order.Master;
    orderOld.Comment = order.Comment;
    if (Enum.IsDefined(typeof(Status), order.Status))
    {
        if (order.Status == Status.NewOrder)
        {
            Notifications.Add($"Order {id} add");
        }
        if (order.Status == Status.InProgress)
        {
            Notifications.Add($"Order {id} in progress");
        }
        if (order.Status == Status.Complete)
        {
            Notifications.Add($"Order {id} complete");
        }
        orderOld.Status = order.Status;
    }
    Notifications.Add($"Order {id} update");
    return Results.Ok("Order update");
});
app.Run();
enum Status
{
    NewOrder, InProgress, Complete
}
class Order
{
    public int Id { get; set; }
    private string view;
    private string model;
    private string description;
    private string fullname;
    private string phone;
    private Status status;
    private string master;
    private string comment;
    public Order() { }
    public Order(string view, string model, string description, string fullName, string phone)
    {
        Id = IdChek++;
        StartDate = DateTime.Now;
        EndDate = DateTime.MinValue;
        View = view;
        Model = model;
        Description = description;
        FullName = fullName;
        Phone = phone;
        Status = Status.NewOrder;
        Master = "Не назначен";
        Comment = "Не добавлены";
    }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string View
    {
        get => view;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the view.");
            }
            view = value;
        }
    }
    public string Model
    {
        get => model;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the model.");
            }
            model = value;
        }
    }
    public string Description
    {
        get => description;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the description.");
            }
            description = value;
        }
    }
    public string FullName
    {
        get => fullname;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the fullname.");
            }
            fullname = value;
        }
    }
    public string Phone
    {
        get => phone;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the phone.");
            }
            phone = value;
        }
    }
    public Status Status
    {
        get => status;
        set
        {
            if (value == Status.Complete)
            {
                EndDate = DateTime.Now;
            }
            if (value == Status.InProgress)
            {
                EndDate = DateTime.MinValue;
            }
            status = value;
        }
    }
    public string Master
    {
        get => master;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the master.");
            }
            master = value;
        }
    }
    public string Comment
    {
        get => comment;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Fill in the name of the comment.");
            }
            comment = value;
        }
    }
    public static int IdChek { get; set; } = 1;
}
class Repository 
{
    public List<Order> Orders { get; set; } = new List<Order>();
    public void AddOrder(Order order)
    {
        Orders.Add(order);
    }
    public Order Read(int id)
    {
        var order = Orders.ToList().Find(x => x.Id == id);
        if (order == null)
        {
            throw new ArgumentException($"Order with id {id} not found.");
        }
        return order;
    }
    public List<Order> ReadAll()
    {
        return Orders.ToList();
    }
    public int CompleteOrders()
    {
        return Orders.Count(o => o.Status == Status.Complete);
    }
    public TimeSpan AverageExecutionTime()
    {
        var completeOrders = Orders.Where(o => o.Status == Status.Complete);
        if (completeOrders.Any())
        {
            return TimeSpan.FromSeconds((double)completeOrders.Average(o => (o.EndDate - o.StartDate).Seconds));
        }
        return TimeSpan.Zero;
    }
    public Dictionary<string, int> ProblemTypeStatictics()
    {
        return Orders.ToList()
           .GroupBy(o => o.Description)
           .ToDictionary(g => g.Key, g => g.Count());
    }
}