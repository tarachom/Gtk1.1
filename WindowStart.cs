using Gtk;
using Npgsql;

namespace GtkTest
{
    class WindowStart : Window
    {
        NpgsqlDataSource? DataSource { get; set; }
        NpgsqlTransaction? Transaction { get; set; }

        ListStore? Store;
        TreeView? treeView;

        public WindowStart() : base("PostgreSQL + GTKSharp")
        {
            SetDefaultSize(1600, 900);
            SetPosition(WindowPosition.Center);

            DeleteEvent += delegate { Program.Quit(); };

            VBox vbox = new VBox();
            Add(vbox);

            #region Кнопки

            //Кнопки
            HBox hBoxButton = new HBox();
            vbox.PackStart(hBoxButton, false, false, 10);

            Button bConnect = new Button("Підключитись до PostgreSQL");
            bConnect.Clicked += OnConnect;
            hBoxButton.PackStart(bConnect, false, false, 10);

            Button bFill = new Button("Заповнити даними");
            bFill.Clicked += OnFill;
            hBoxButton.PackStart(bFill, false, false, 10);

            Button bAdd = new Button("Додати один запис");
            bAdd.Clicked += OnAdd;
            hBoxButton.PackStart(bAdd, false, false, 10);

            Button bSave = new Button("Зберегти зміни");
            bSave.Clicked += OnSave;
            hBoxButton.PackStart(bSave, false, false, 10);

            Button bDelete = new Button("Видалити");
            bDelete.Clicked += OnDelete;
            hBoxButton.PackStart(bDelete, false, false, 10);

            #endregion

            //Список
            HBox hboxTree = new HBox();
            vbox.PackStart(hboxTree, true, true, 0);

            AddColumn();

            ScrolledWindow scroll = new ScrolledWindow() { ShadowType = ShadowType.In };
            scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            scroll.Add(treeView);

            hboxTree.PackStart(scroll, true, true, 10);

            ShowAll();
        }

        enum Columns
        {
            image,
            id,
            name,
            desc,
            info
        }

        void AddColumn()
        {
            Store = new ListStore
            (
                typeof(Gdk.Pixbuf),
                typeof(int),       //id
                typeof(string),    //name
                typeof(string),    //desc
                typeof(string)     //info
            );

            treeView = new TreeView(Store);
            treeView.Selection.Mode = SelectionMode.Multiple;

            treeView.AppendColumn(new TreeViewColumn("", new CellRendererPixbuf(), "pixbuf", (int)Columns.image));
            treeView.AppendColumn(new TreeViewColumn("id", new CellRendererText(), "text", (int)Columns.id) { MinWidth = 100 });

            CellRendererText nameRendererText = new CellRendererText() { Editable = true };
            nameRendererText.Edited += OnNameEdited;

            treeView.AppendColumn(new TreeViewColumn("name", nameRendererText, "text", (int)Columns.name) { MinWidth = 300 });

            CellRendererText descRendererText = new CellRendererText() { Editable = true };
            descRendererText.Edited += OnDescEdited;

            treeView.AppendColumn(new TreeViewColumn("desc", descRendererText, "text", (int)Columns.desc) { MinWidth = 300 });

            treeView.AppendColumn(new TreeViewColumn("info", new CellRendererText(), "text", (int)Columns.info) { MinWidth = 100 });
        }

        void OnNameEdited(object sender, EditedArgs args)
        {
            CellRenderer cellRender = (CellRenderer)sender;

            TreeIter iter;
            Store!.GetIterFromString(out iter, args.Path);
            Store!.SetValue(iter, (int)Columns.name, args.NewText);
        }

        void OnDescEdited(object sender, EditedArgs args)
        {
            CellRenderer cellRender = (CellRenderer)sender;

            TreeIter iter;
            Store!.GetIterFromString(out iter, args.Path);
            Store!.SetValue(iter, (int)Columns.desc, args.NewText);
        }

        void OnConnect(object? sender, EventArgs args)
        {
            string Server = "localhost";
            string UserId = "postgres";
            string Password = "1";
            int Port = 5432;
            string Database = "test";

            string conString = $"Server={Server};Username={UserId};Password={Password};Port={Port};Database={Database};SSLMode=Prefer;";

            NpgsqlDataSourceBuilder dataBuilder = new NpgsqlDataSourceBuilder(conString);
            dataBuilder.MapComposite<UuidAndText>("uuidtext");

            DataSource = dataBuilder.Build();

            OnStart();

            OnFill(this, new EventArgs());
        }

        void OnStart()
        {
            if (DataSource != null)
            {
                //Перевірити чи вже є такий тип даних
                string query = "SELECT 'Exist' FROM pg_type WHERE typname = 'uuidtext'";
                NpgsqlCommand command = DataSource.CreateCommand(query);
                object? result = command.ExecuteScalar();

                if (!(result != null && result.ToString() == "Exist"))
                {
                    //Створити новий композитний тип uuidtext
                    command.CommandText = @"
CREATE TYPE uuidtext AS 
(
    uuid uuid, 
    text text
)";
                    command.ExecuteNonQuery();

                    //Додати колонку info в таблицю tab1 типу uuidtext
                    command.CommandText = "ALTER TABLE tab1 ADD COLUMN info uuidtext";
                    command.ExecuteNonQuery();

                    //Перечитати типи
                    DataSource.OpenConnection().ReloadTypes();
                }
            }
        }

        #region Transaction

        void BeginTransaction()
        {
            if (DataSource != null)
            {
                Transaction = DataSource.OpenConnection().BeginTransaction();
            }
        }

        void CommitTransaction()
        {
            if (Transaction != null)
            {
                Transaction.Commit();
                Transaction.Connection?.Close();
            }
        }

        void RollbackTransaction()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
                Transaction.Connection?.Close();
            }
        }

        #endregion

        void OnFill(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                Store!.Clear();

                NpgsqlCommand command = DataSource.CreateCommand(
                    "SELECT id, name, \"desc\", info FROM tab1 ORDER BY id");

                NpgsqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int id = (int)reader["id"];
                    string name = reader["name"].ToString() ?? "";
                    string desc = reader["desc"].ToString() ?? "";
                    UuidAndText info = reader["info"] != DBNull.Value ? (UuidAndText)reader["info"] : new UuidAndText();

                    Store!.AppendValues(new Gdk.Pixbuf("doc.png"), id, name, desc, info.ToString());
                }

                reader.Close();
            }
        }

        void OnAdd(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                NpgsqlCommand command = DataSource.CreateCommand(
                    "INSERT INTO tab1 (name, \"desc\", info) VALUES (@name, @desc, @info)");

                command.Parameters.AddWithValue("name", "test");
                command.Parameters.AddWithValue("desc", "test");
                command.Parameters.AddWithValue("info", new UuidAndText(Guid.NewGuid(), "info text"));

                command.ExecuteNonQuery();

                OnFill(this, new EventArgs());
            }
        }

        void OnSave(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                BeginTransaction();

                NpgsqlCommand command = DataSource.CreateCommand(
                    "UPDATE tab1 SET name = @name, \"desc\" = @desc, info = @info WHERE id = @id");

                TreeIter iter;
                if (Store!.GetIterFirst(out iter))
                    do
                    {
                        int id = (int)Store!.GetValue(iter, (int)Columns.id);
                        string name = (string)Store!.GetValue(iter, (int)Columns.name);
                        string desc = (string)Store!.GetValue(iter, (int)Columns.desc);

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("id", id);
                        command.Parameters.AddWithValue("name", name);
                        command.Parameters.AddWithValue("desc", desc);
                        command.Parameters.AddWithValue("info", new UuidAndText(Guid.NewGuid(), name + " - " + desc));

                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            RollbackTransaction();

                            Console.WriteLine(ex.Message);
                            return;
                        }
                    }
                    while (Store.IterNext(ref iter));

                CommitTransaction();

                OnFill(this, new EventArgs());
            }
        }

        void OnDelete(object? sender, EventArgs args)
        {
            if (DataSource != null)
            {
                if (treeView!.Selection.CountSelectedRows() != 0)
                {
                    NpgsqlCommand command = DataSource.CreateCommand(
                        "DELETE FROM tab1 WHERE id = @id");

                    TreePath[] selectionRows = treeView.Selection.GetSelectedRows();

                    foreach (TreePath itemPath in selectionRows)
                    {
                        TreeIter iter;
                        treeView.Model.GetIter(out iter, itemPath);

                        int id = (int)treeView.Model.GetValue(iter, (int)Columns.id);

                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("id", id);

                        command.ExecuteNonQuery();
                    }

                    OnFill(this, new EventArgs());
                }
            }
        }

    }
}