namespace Ascension.Controller

open System
open System.IO
open Ascension
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Models
open System.Linq
open Models
open Models.Attributes
open System.Collections.Generic
open Selector
open Checks

type CreateModel(modelType : Type, message : string) =
        member this.ModelType = modelType
        member this.Message = message
        
type UpdateModel(model : IModel, message : string) =
    member this.Model = model
    member this.Message = message
    
type AdminsModel(users : List<User>, message : string) =
    member this.Users = users
    member this.Message = message

type AdminController() =
    inherit Controller()
    
    let isAdmin (context : HttpContext) =
        if context.Session.Keys.Contains("id")
            then
                use dbContext = new ApplicationContext()
                let id = context.Session.GetInt32("id") |> int
                let user = dbContext.User.First(fun u -> u.Id = id)
                user.IsAdmin
            else
                false
                
    let getPath (file : IFormFile) =
        if file <> null
        then
            "/img/" + file.FileName
        else
            ""
    
    let createFile (file : IFormFile) =
        if file <> null
        then
            let path = "wwwroot" + getPath file 
            use fileStream = new FileStream(path, FileMode.Create)
            file.CopyTo(fileStream)
    
    [<HttpGet>]
    member this.Index() =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let model = context
                          .GetType()
                          .GetProperties()
                          .Where(fun p -> p.GetCustomAttributes(typedefof<DisplayedInAdminPanelAttribute>, false).Any())
                          .Select(fun p -> p.Name)
            this.View(model) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
    [<HttpGet>]
    member this.Models(name : string) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            this.View(getModelsWithoutRelations name) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
    [<HttpGet>]
    member this.Orders() =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let orders = context.Order.OrderByDescending(fun o -> o.Id).ToList()
            this.View(orders) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
    [<HttpPost>]
    member this.Orders(id : int, status : Status) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let order = context.Order.FirstOrDefault(fun o -> o.Id = id)
            if order = null
            then
                this.NotFound() :> ActionResult
            else
                order.Status <- status
                context.SaveChanges() |> ignore
                this.Redirect("Orders") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
    [<HttpGet>]
    member this.Admins() =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let users = context.User.ToList()
            this.View("Admins", AdminsModel(users, String.Empty)) :> ActionResult
        else
            this.Forbid() :> ActionResult
          
    [<HttpPost>]
    member this.GrantRights(userId : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let user = context.User.FirstOrDefault(fun u -> u.Id = userId)
            let users = context.User.ToList()
            if user = null
            then
                this.View("Admins", AdminsModel(users, "There is no such user")) :> ActionResult
            else
                user.IsAdmin <- true
                context.SaveChanges() |> ignore
                this.View("Admins", AdminsModel(users, String.Empty)) :> ActionResult
        else
            this.Forbid() :> ActionResult
            
    [<HttpPost>]
    member this.RemoveRights(userId : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let user = context.User.FirstOrDefault(fun u -> u.Id = userId)
            let users = context.User.ToList()
            if user = null
            then
                this.View("Admins", AdminsModel(users, "There is no such user")) :> ActionResult
            else
                user.IsAdmin <- false
                context.SaveChanges() |> ignore
                this.View("Admins", AdminsModel(users, String.Empty)) :> ActionResult
        else
            this.Forbid() :> ActionResult
            
    
    //Create
    [<HttpGet>]     
    member this.Create(name : string) =
        if isAdmin this.HttpContext
        then
             this.View(CreateModel(getModelType name, String.Empty)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
    [<HttpPost>]
    member this.CreateSuperCategory(formModel : SuperCategoryModel, file : IFormFile) =
        if isAdmin this.HttpContext
        then
            formModel.ImagePath <- getPath file
            let checkResult = checkSuperCategory formModel
            let createSuperCategory (model : SuperCategoryModel) =
                use context = new ApplicationContext()
                createFile file
                context.SuperCategory.Add(SuperCategory().Update(model.Name, model.ImagePath, context)) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect("/admin/models?name=SuperCategory") :> ActionResult

            match checkResult with
            | Ok(checkedModel) -> createSuperCategory checkedModel
            | Bad(message) -> this.View("Create", CreateModel(typeof<SuperCategory>, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
    [<HttpPost>]
    member this.CreateCategory(formModel : CategoryModel, file : IFormFile) =
        if isAdmin this.HttpContext
        then
            formModel.ImagePath <- getPath file
            let checkResult = checkCategory formModel
            let createCategory (model : CategoryModel) =
                use context = new ApplicationContext()
                createFile file
                context.Category.Add(Category().Update(model.Name, model.ImagePath, model.SuperCategory, context)) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect("/admin/models?name=Category") :> ActionResult
                
            match checkResult with
            | Ok(checkedModel) -> createCategory checkedModel
            | Bad(message) -> this.View("Create", CreateModel(typeof<Category>, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
    [<HttpPost>]
    member this.CreateSpecification(formModel : SpecificationModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkSpecification formModel
            let createSpecification (model : SpecificationModel) =
                use context = new ApplicationContext()
                context.Specification.Add(Specification().Update(model.Name, model.Category, context)) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect("/admin/models?name=Specification") :> ActionResult
            
            match checkResult with
            | Ok(checkedModel) -> createSpecification checkedModel
            | Bad(message) -> this.View("Create", CreateModel(typeof<Specification>, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.CreateSpecificationOption(formModel : SpecificationOptionModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkSpecificationOption formModel
            let createSpecificationOption (model : SpecificationOptionModel) =
                use context = new ApplicationContext()
                context.SpecificationOption.Add(SpecificationOption().Update(model.Name, model.Specification, model.Products, context)) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect("/admin/models?name=SpecificationOption") :> ActionResult
                
            match checkResult with
            | Ok(checkedModel) -> createSpecificationOption checkedModel
            | Bad(message) -> this.View("Create", CreateModel(typeof<SpecificationOption>, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.CreateProduct(formModel : ProductModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkProduct formModel
            let createProduct (model : ProductModel) =
                use context = new ApplicationContext()
                context.Product.Add(Product().Update(model.Name, model.Cost, model.Description, model.Category, model.SpecificationOptions, context)) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect("/admin/models?name=Product") :> ActionResult
                
            match checkResult with
            | Ok(checkedModel) -> createProduct checkedModel
            | Bad(message) -> this.View("Create", CreateModel(typeof<Product>, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.CreateImage(formModel : ImageModel, file : IFormFile) =
        if isAdmin this.HttpContext
        then
            formModel.Path <- getPath file
            let checkResult = checkImage formModel
            let createImage (model : ImageModel) =
                use context = new ApplicationContext()
                createFile file
                context.Image.Add(Image().Update(formModel.Path, model.Product, context)) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect("/admin/models?name=Image") :> ActionResult
                
            match checkResult with
            | Ok(checkedModel) -> createImage checkedModel
            | Bad(message) -> this.View("Create", CreateModel(typeof<Image>, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
        
    //Read    
    [<HttpGet>]
    member this.Read(name : string, id : int) =
        if isAdmin this.HttpContext
        then
            let model = getModelWithRelations name id
            this.View(model) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    
    //Update
    [<HttpGet>]    
    member this.Update(name : string, id : int) =
        if isAdmin this.HttpContext
        then
            let model = getModelWithRelations name id
            this.View(UpdateModel(model, String.Empty)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
    [<HttpPost>]    
    member this.UpdateSuperCategory(formModel : SuperCategoryModel, file : IFormFile) =
        if isAdmin this.HttpContext
        then
            formModel.ImagePath <- getPath file
            let checkResult = checkSuperCategory formModel
            let updateSuperCategory (model : SuperCategoryModel) = 
                use context = new ApplicationContext()
                createFile file
                context
                    .SuperCategory
                    .First(fun sc -> sc.Id = model.Id)
                    .Update(model.Name, model.ImagePath, context) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect($"/admin/read?name=SuperCategory&id={model.Id}") :> ActionResult
                
            match checkResult with
            | Ok(checkedModel) -> updateSuperCategory checkedModel
            | Bad(message) ->
                let model = getModelWithRelations "SuperCategory" formModel.Id
                this.View("Update", UpdateModel(model, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
    [<HttpPost>]
    member this.UpdateCategory(formModel : CategoryModel, file : IFormFile) =
        if isAdmin this.HttpContext
        then
            formModel.ImagePath <- getPath file
            let checkResult = checkCategory formModel
            let updateCategory (model : CategoryModel) =
                use context = new ApplicationContext()
                createFile file
                context
                    .Category
                    .First(fun c -> c.Id = model.Id)
                    .Update(model.Name, model.ImagePath, model.SuperCategory, context) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect($"/admin/read?name=Category&id={model.Id}") :> ActionResult
            
            match checkResult with
            | Ok(checkedModel) -> updateCategory checkedModel
            | Bad(message) ->
                let model = getModelWithRelations "Category" formModel.Id
                this.View("Update", UpdateModel(model, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.UpdateSpecification(formModel : SpecificationModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkSpecification formModel
            let updateSpecification (model : SpecificationModel) =
                use context = new ApplicationContext()
                context
                    .Specification
                    .First(fun s -> s.Id = model.Id)
                    .Update(model.Name, model.Category, context) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect($"/admin/read?name=Specification&id={model.Id}") :> ActionResult
            
            match checkResult with
            | Ok(checkedModel) -> updateSpecification checkedModel
            | Bad(message) ->
                let model = getModelWithRelations "Specification" formModel.Id
                this.View("Update", UpdateModel(model, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.UpdateSpecificationOption(formModel : SpecificationOptionModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkSpecificationOption formModel
            let updateSpecificationOption (model : SpecificationOptionModel) =
                use context = new ApplicationContext()
                context
                    .SpecificationOption
                    .First(fun sOp -> sOp.Id = model.Id)
                    .Update(model.Name, model.Specification, model.Products, context) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect($"/admin/read?name=SpecificationOption&id={model.Id}") :> ActionResult
            
            match checkResult with
            | Ok(checkedModel) -> updateSpecificationOption checkedModel
            | Bad(message) ->
                let model = getModelWithRelations "SpecificationOption" formModel.Id
                this.View("Update", UpdateModel(model, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.UpdateProduct(formModel : ProductModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkProduct formModel
            let updateProduct (model : ProductModel) =
                use context = new ApplicationContext()
                context
                    .Product
                    .First(fun p -> p.Id = model.Id)
                    .Update(model.Name, model.Cost, model.Description, model.Category, model.SpecificationOptions, context) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect($"/admin/read?name=Product&id={model.Id}") :> ActionResult
                
            match checkResult with
            | Ok(checkedModel) -> updateProduct checkedModel
            | Bad(message) ->
                let model = getModelWithRelations "Product" formModel.Id
                this.View("Update", UpdateModel(model, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
    [<HttpPost>]
    member this.UpdateImage(formModel : ImageModel) =
        if isAdmin this.HttpContext
        then
            let checkResult = checkImage formModel
            let updateImage (model : ImageModel) =
                use context = new ApplicationContext()
                context
                    .Image
                    .First(fun i -> i.Id = formModel.Id)
                    .Update(model.Path, model.Product, context) |> ignore
                context.SaveChanges() |> ignore
                this.Response.StatusCode = 200 |> ignore
                this.Redirect($"/admin/read?name=image&id={model.Id}") :> ActionResult
            
            match checkResult with
            | Ok(checkedModel) -> updateImage checkedModel
            | Bad(message) ->
                let model = getModelWithRelations "Image" formModel.Id
                this.View("Update", UpdateModel(model, message)) :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
        
    
    //Delete
    [<HttpPost>]    
    member this.DeleteSuperCategory(id : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let modelToDelete = SuperCategory.GetModel(id) :?> SuperCategory
            context.SuperCategory.Remove(modelToDelete) |> ignore
            context.SaveChanges() |> ignore
            this.Response.StatusCode = 200 |> ignore
            this.Redirect("/admin/models?name=SuperCategory") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
    [<HttpPost>]
    member this.DeleteCategory(id : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let modelToDelete = Category.GetModel(id) :?> Category
            context.Category.Remove(modelToDelete) |> ignore
            context.SaveChanges() |> ignore
            this.Response.StatusCode = 200 |> ignore
            this.Redirect("/admin/models?name=Category") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
            
        
    [<HttpPost>]
    member this.DeleteSpecification(id : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let modelToDelete = Specification.GetModel(id) :?> Specification
            context.Specification.Remove(modelToDelete) |> ignore
            context.SaveChanges() |> ignore
            this.Response.StatusCode = 200 |> ignore
            this.Redirect("/admin/models?name=Specification") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.DeleteSpecificationOption(id : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let modelToDelete = SpecificationOption.GetModel(id) :?> SpecificationOption
            context.SpecificationOption.Remove(modelToDelete) |> ignore
            context.SaveChanges() |> ignore
            this.Response.StatusCode = 200 |> ignore
            this.Redirect("/admin/models?name=SpecificationOption") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.DeleteProduct(id : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let modelToDelete = Product.GetModel(id) :?> Product
            context.Product.Remove(modelToDelete) |> ignore
            context.SaveChanges() |> ignore
            this.Response.StatusCode = 200 |> ignore
            this.Redirect("/admin/models?name=Product") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult
    
    [<HttpPost>]
    member this.DeleteImage(id : int) =
        if isAdmin this.HttpContext
        then
            use context = new ApplicationContext()
            let modelToDelete = Image.GetModel(id) :?> Image
            context.Image.Remove(modelToDelete) |> ignore
            context.SaveChanges() |> ignore
            this.Response.StatusCode = 200 |> ignore
            this.Redirect("/admin/models?name=Image") :> ActionResult
        else
            this.StatusCode(403) :> ActionResult