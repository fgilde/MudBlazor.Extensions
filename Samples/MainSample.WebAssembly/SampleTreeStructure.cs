using Nextended.Core.Types;

namespace MainSample.WebAssembly;

public class SampleTreeStructure
{
    public static HashSet<SampleTreeItem> GetItems()
    {
        return
        [
            ..new[]
            {
                new SampleTreeItem("Countries")
        {
            Children =
            [
                new("Europe")
                {
                    Children =
                    [
                        new("Germany"),
                        new("France"),
                        new("Italy")
                        {
                            Children =
                            [
                                new("Rome"),
                                new("Milan"),
                                new("Naples")
                                {
                                    Children =
                                    [
                                        new("Historic Center"),
                                        new("Posillipo"),
                                        new("Vomero")
                                        {
                                            Children =
                                            [
                                                new("Schools"),
                                                new("Hospitals"),
                                                new("Parks")
                                                {
                                                    Children =
                                                    [
                                                        new("Park 1"),
                                                        new("Park 2"),
                                                        new("Park 3")
                                                        {
                                                            Children =
                                                            [
                                                                new("North Area"),
                                                                new("South Area"),
                                                                new("Central Area")
                                                                {
                                                                    Children =
                                                                    [
                                                                        new("Subarea 1"),
                                                                        new("Subarea 2"),
                                                                        new("Subarea 3")
                                                                    ]
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        },
                new SampleTreeItem("Technologies")
        {
            Children =
            [
                new("Programming Languages")
                {
                    Children =
                    [
                        new("Python"),
                        new("JavaScript"),
                        new("C#")
                        {
                            Children =
                            [
                                new("ASP.NET"),
                                new("Entity Framework"),
                                new("Xamarin")
                                {
                                    Children =
                                    [
                                        new("Xamarin.Forms"),
                                        new("Xamarin.Android"),
                                        new("Xamarin.iOS")
                                        {
                                            Children =
                                            [
                                                new("UI Components"),
                                                new("Platform Specifics"),
                                                new("Libraries")
                                                {
                                                    Children =
                                                    [
                                                        new("Library 1"),
                                                        new("Library 2"),
                                                        new("Library 3")
                                                        {
                                                            Children =
                                                            [
                                                                new("Feature 1"),
                                                                new("Feature 2"),
                                                                new("Feature 3")
                                                                {
                                                                    Children =
                                                                    [
                                                                        new("Subfeature 1"),
                                                                        new("Subfeature 2"),
                                                                        new("Subfeature 3")
                                                                    ]
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                },
                new("Frameworks")
                {
                    Children =
                    [
                        new("React"),
                        new("Angular"),
                        new("Vue")
                        {
                            Children =
                            [
                                new("Vuex"),
                                new("Vue Router"),
                                new("Nuxt")
                                {
                                    Children =
                                    [
                                        new("Modules"),
                                        new("Plugins"),
                                        new("Middlewares")
                                        {
                                            Children =
                                            [
                                                new("Auth Middleware"),
                                                new("Logging Middleware"),
                                                new("Error Handling Middleware")
                                                {
                                                    Children =
                                                    [
                                                        new("Error Page 1"),
                                                        new("Error Page 2"),
                                                        new("Error Page 3")
                                                        {
                                                            Children =
                                                            [
                                                                new("Details 1"),
                                                                new("Details 2"),
                                                                new("Details 3")
                                                                {
                                                                    Children =
                                                                    [
                                                                        new("Info 1"),
                                                                        new("Info 2"),
                                                                        new("Info 3")
                                                                    ]
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        },
                new SampleTreeItem("Async load sample 1", LoadChildren ),
                new SampleTreeItem("Item 1")
                {
                    Children =
                    [
                        new("Item 1.1"),
                        new("Item 1.2"),
                        new("Item 1.3")
                        {
                            Children =
                            [
                                new("Item 1.3.1"),
                                new("Item 1.3.2"),
                                new("Item 1.3.3")
                            ]
                        }
                    ]
                },
                new SampleTreeItem("Item 2")
                {
                    Children =
                    [
                        new("Item 2.1"),
                        new("Item 2.2"),
                        new("Item 2.3")
                        {
                            Children =
                            [
                                new("Item 2.3.1"),
                                new("Item 2.3.2"),
                                new("Item 2.3.3")
                            ]
                        }
                    ]
                },
                new SampleTreeItem("Item 3")
                {
                    Children =
                    [
                        new("Item 3.1"),
                        new("Item 3.2"),
                        new("Item 3.3")
                        {
                            Children =
                            [
                                new("Item 3.3.1"),
                                new("Item 3.3.2"),
                                new("Item 3.3.3")
                            ]
                        }
                    ]
                },
                new SampleTreeItem("Item 4")
                {
                    Children =
                    [
                        new("Item 4.1"),
                        new("Item 4.2")
                        {
                            Children =                             [
                                new("Item 4.2.1"),
                                new("Item 4.2.2")
                                {
                                    Children = [
                                        new("Item 4.2.2.1"),
                                        new("Item 4.2.2.2"),

                                        ]
                                },
                                new("Item 4.2.3")
                            ]
                        },
                        new("Item 4.3")
                        {
                            Children =
                            [
                                new("Item 4.3.1"),
                                new("Item 4.3.2"),
                                new("Item 4.3.3")
                            ]
                        }
                    ]
                }
            }
        ];
    }

    private static async Task<HashSet<SampleTreeItem>> LoadChildren(SampleTreeItem arg, CancellationToken token)
    {
        Console.WriteLine("Loading children for " + arg.Name);
        await Task.Delay(5000);
        return
        [
            new SampleTreeItem("Async load sample 1.1"),
            new SampleTreeItem("Async load sample 1.2"),
            new SampleTreeItem("Async load sample 1.3")
            {
                Children =
                [
                    new SampleTreeItem("Async load sample 1.3.1"),
                    new SampleTreeItem("Async load sample 1.3.2"),
                    new SampleTreeItem("Async load sample 1.3.3")
                ]
            }
        ];
    }
}

public class SampleTreeItem : Hierarchical<SampleTreeItem>
{
    public string Name { get; }

    public SampleTreeItem()
    {}

    public SampleTreeItem(string name)
    {
        Name = name;
    }
    
    public SampleTreeItem(string name, Func<SampleTreeItem, CancellationToken, Task<HashSet<SampleTreeItem>>> load)
    {
        LoadChildrenFunc = load;
        Name = name;
    }

    public override string ToString() => Name;
}
