using CampusEats.Api.Data;
using CampusEats.Api.Dtos;
using CampusEats.Api.Models;

namespace CampusEats.Api.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _db;

    public MenuService(AppDbContext db)
    {
        _db = db;
    }

    private static MenuItemDto ToDto(MenuItem m) =>
        new(m.Id, m.Name, m.Price, m.Category, m.Available);

    public IEnumerable<MenuItemDto> GetAll()
    {
        return _db.MenuItems
            .OrderBy(m => m.Id)
            .ToList()
            .Select(ToDto);
    }

    public MenuItemDto? GetById(int id)
    {
        var menuItem = _db.MenuItems.Find(id);

        return menuItem is null ? null : ToDto(menuItem);
    }

    public MenuItemDto Create(CreateMenuItemDto dto)
    {
        var item = new MenuItem
        {
            Name = dto.Name,
            Price = dto.Price,
            Category = dto.Category,
            Available = true
        };

        _db.MenuItems.Add(item);
        _db.SaveChanges();

        return ToDto(item);
    }

    public bool Update(int id, CreateMenuItemDto dto)
    {
        var item = _db.MenuItems.Find(id);

        if (item is null)
            return false;

        item.Name = dto.Name;
        item.Price = dto.Price;
        item.Category = dto.Category;

        _db.SaveChanges();

        return true;
    }

    public bool Delete(int id)
    {
        var item = _db.MenuItems.Find(id);

        if (item is null)
            return false;

        _db.MenuItems.Remove(item);
        _db.SaveChanges();

        return true;
    }
}