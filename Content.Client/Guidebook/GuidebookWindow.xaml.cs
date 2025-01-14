using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.ContentPack;

namespace Content.Client.Guidebook;

[GenerateTypedNameReferences]
public sealed partial class GuidebookWindow : FancyWindow
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    private List<GuideEntry> _entries = new();

    public GuidebookWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        GuideSelect.OnItemSelected += GuideSelectOnOnItemSelected;
    }

    private void GuideSelectOnOnItemSelected()
    {
        var entry = (GuideEntry) GuideSelect.Selected!.Metadata!;

        var text = _resourceManager.ContentFileReadText(entry.Text).ReadToEnd();

        GuideContainer.RemoveAllChildren();

        GuideContainer.AddChild(new Label()
        {
            StyleClasses = { "LabelHeadingBigger" },
            Text = entry.Name
        });

        GuideContainer.AddChild(new Document(text));

        GuideContainer.MaxWidth = this.Size.X * (2.0f / 3.0f) - 20.0f * UIScale;

        return;

        LayoutGuidebook(text, GuideContainer);
    }

    public void UpdateGuides(List<GuideEntry> entries)
    {
        _entries = entries;
        RedrawTree();
    }

    private void RedrawTree()
    {
        var map = new Dictionary<string, Tree.Item>();
        var unassigned = _entries.OrderBy(x => x.Priority).ThenBy(x => x.Parent ?? "").ThenBy(x => x.Name).ToList();
        var i = 0;
        var bulletFirst = false;
        GuideSelect.Clear();

        while (unassigned.Count != 0 && i < 128)
        {
            for (var j = 0; j < unassigned.Count; j++)
            {
                var entry = unassigned[j];

                if (entry.Parent != null && !map.ContainsKey(entry.Parent))
                    continue;

                Tree.Item? parent = null;

                if (entry.Parent is not null)
                    map.TryGetValue(entry.Parent, out parent);

                var item = GuideSelect.CreateItem(parent);
                item.Metadata = entry;
                if (!bulletFirst)
                    item.Text = entry.Name;
                else
                    item.Text = $"› {entry.Name}";

                bulletFirst = true;

                map.Add(entry.Id, item);

                unassigned.RemoveAt(j);
                j--;
            }

            i++;
        }

        if (unassigned.Count != 0)
        {
            Logger.Error("The following guides are missing their parent guides:");
            foreach (var guide in unassigned)
            {
                Logger.Error($"Guide: {guide.Id} ({guide.Name}), which belongs to {guide.Parent}");
            }
        }
    }
}
