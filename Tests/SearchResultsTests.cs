using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace CN_GreenLumaGUI.Tests;

public class SearchResultsTests
{
    [Fact]
    public void SteamTopThreeStayAheadOfMatchesRegardlessOfType()
    {
        var apps = new List<AppModel>
        {
            App(1, "Other DLC", false),
            App(2, "Another game"),
            App(3, "Space adventure", false),
            App(4, "Space"),
            App(5, "Space expansion", false)
        };

        AssertOrder(Sort("Space", apps), 1, 2, 3, 4, 5);
    }

    [Fact]
    public void SteamRankDeterminesTopThreeEvenWhenDetailsArriveOutOfOrder()
    {
        var apps = new List<AppModel>
        {
            App(5, "Space"),
            App(3, "Other DLC", false),
            App(4, "Space expansion"),
            App(2, "Other game"),
            App(1, "Another DLC", false)
        };

        for (int i = 0; i < apps.Count; i++) apps[i].Index = i + 1;

        AssertOrder(Sort("Space", apps), 5, 4, 2, 1, 3);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, apps.Select(app => app.Index));
    }

    [Fact]
    public void EachMatchTierPromotesOnlyFiveAndOverflowIsReclassifiedByType()
    {
        var apps = TopThree();
        apps.Add(App(4, "Other DLC", false));
        apps.AddRange(Enumerable.Range(5, 5).Select(rank => App(rank, "Space")));
        apps.Add(App(10, "Space", false));
        apps.Add(App(11, "Space"));
        apps.AddRange(Enumerable.Range(12, 5).Select(rank => App(rank, "Space expansion", false)));
        apps.Add(App(17, "Space extra", false));
        apps.Add(App(18, "Space sequel"));
        apps.Add(App(19, "Other game"));
        apps.Reverse();

        AssertOrder(Sort("Space", apps),
            1, 2, 3, 5, 6, 7, 8, 9, 12, 13, 14, 15, 16, 11, 18, 19, 4, 10, 17);
    }

    [Fact]
    public void TopThreeDoNotConsumeMatchTierQuotas()
    {
        var apps = new List<AppModel>
        {
            App(1, "Space"),
            App(2, "Space", false),
            App(3, "Space"),
            App(4, "Other game")
        };
        apps.AddRange(Enumerable.Range(5, 6).Select(rank => App(rank, "Space")));

        AssertOrder(Sort("Space", apps), 1, 2, 3, 5, 6, 7, 8, 9, 4, 10);
    }

    [Fact]
    public void OnlyPrefixesArePromotedAndNormalizationIsPreserved()
    {
        var apps = TopThree();
        apps.Add(App(4, "Lost in Space"));
        apps.Add(App(5, "s p_a\tce expansion", false));
        apps.Add(App(6, "SPACE", false));
        apps.Add(App(7, "Other game"));

        AssertOrder(Sort("s_pa ce", apps), 1, 2, 3, 6, 5, 4, 7);
    }

    [Fact]
    public void PrefixesShorterThanFiveCharactersAreNotPromoted()
    {
        var apps = TopThree();
        apps.Add(App(4, "Other game"));
        apps.Add(App(5, "Space expansion", false));
        apps.Add(App(6, "SPAC", false));

        AssertOrder(Sort("spac", apps), 1, 2, 3, 6, 4, 5);
    }

    [Fact]
    public void NextPageDoesNotGetAnotherPinnedTopThreeOrResetQuotas()
    {
        var apps = TopThree();
        apps.AddRange(Enumerable.Range(4, 5).Select(rank => App(rank, "Space")));
        apps.Add(App(9, "Other game"));
        var viewModel = CreateViewModel("Space");
        viewModel.AppsList = apps;
        apps = new List<AppModel>(apps)
        {
            App(26, "Other DLC", false),
            App(27, "Space", false),
            App(28, "Space expansion", false),
            App(29, "Other game")
        };
        viewModel.AppsList = apps;

        AssertOrder(viewModel.AppsList, 1, 2, 3, 4, 5, 6, 7, 8, 28, 9, 29, 26, 27);
    }

    [Fact]
    public void LaterArrivingSteamTopThreeDisplacePreviouslyDisplayedMatches()
    {
        var viewModel = CreateViewModel("Space");
        var apps = new List<AppModel> { App(4, "Space"), App(5, "Other game") };
        viewModel.AppsList = apps;
        AssertOrder(viewModel.AppsList, 4, 5);
        apps = new List<AppModel>(apps) { App(3, "Other DLC", false), App(1, "Other game"), App(2, "Other DLC", false) };
        viewModel.AppsList = apps;

        AssertOrder(viewModel.AppsList, 1, 2, 3, 4, 5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ThreeOrFewerResultsKeepSteamOrder(int count)
    {
        var apps = Enumerable.Range(1, count).Reverse()
            .Select(rank => App(rank, rank == 2 ? "Space" : "Other DLC", false)).ToList();

        AssertOrder(Sort("Space", apps), Enumerable.Range(1, count).ToArray());
    }

    [Fact]
    public void NewSearchRecomputesQuotasAndClearedResultsStayEmpty()
    {
        var viewModel = CreateViewModel("Space");
        var apps = TopThree();
        apps.AddRange(Enumerable.Range(4, 6).Select(rank => App(rank, "Space", false)));
        viewModel.AppsList = apps;
        viewModel.AppsList = new List<AppModel>();
        Assert.Empty(viewModel.AppsList);
        apps = TopThree();
        apps.Add(App(4, "Other game"));
        apps.Add(App(5, "Space", false));
        viewModel.AppsList = apps;

        AssertOrder(viewModel.AppsList, 1, 2, 3, 5, 4);
    }

    [Fact]
    public void DisplayNumbersFollowPrioritizedOrderWithoutChangingSourceIndexes()
    {
        var apps = TopThree();
        apps.AddRange(new[]
        {
            App(4, "Other DLC", false), App(5, "Other game"), App(6, "Space"),
            App(7, "Space expansion", false), App(8, "Another game")
        });

        var sorted = Sort("Space", apps);

        AssertOrder(sorted, 1, 2, 3, 6, 7, 5, 8, 4);
        AssertDisplayNumbers(sorted);
        Assert.Equal(Enumerable.Range(1, 8), apps.Select(app => app.Index));
        Assert.Equal(Enumerable.Range(1, 8), apps.Select(app => app.SearchResultIndex));
    }

    [Fact]
    public void NextPageRenumbersAllDisplayedRowsContinuously()
    {
        var viewModel = CreateViewModel("Space");
        var apps = TopThree();
        apps.AddRange(new[] { App(4, "Other DLC", false), App(5, "Space"), App(6, "Other game") });
        viewModel.AppsList = apps;
        apps = new List<AppModel>(apps)
        {
            App(26, "Space expansion", false), App(27, "Space", false), App(28, "Other game")
        };
        viewModel.AppsList = apps;

        AssertOrder(viewModel.AppsList, 1, 2, 3, 5, 27, 26, 6, 28, 4);
        AssertDisplayNumbers(viewModel.AppsList);
        Assert.Equal(28, apps.Last().Index);
        Assert.Equal(28, apps.Last().SearchResultIndex);
    }

    [Fact]
    public void RenumberingExistingRowsNotifiesTheirBindings()
    {
        var viewModel = CreateViewModel("Space");
        var promoted = App(5, "Space");
        var other = App(4, "Other DLC", false);
        viewModel.AppsList = new List<AppModel> { other, promoted };
        var changedProperties = new List<string?>();
        promoted.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        viewModel.AppsList = new List<AppModel> { other, promoted, App(1, "Other game") };

        AssertOrder(viewModel.AppsList, 1, 5, 4);
        AssertDisplayNumbers(viewModel.AppsList);
        Assert.Contains(nameof(AppModel.DisplayIndex), changedProperties);
    }

    private static List<AppModel> TopThree() => new()
    {
        App(1, "Unrelated DLC", false), App(2, "Other game"), App(3, "Another DLC", false)
    };

    private static AppModel App(int rank, string name, bool isGame = true)
    {
        return new AppModel { Index = rank, SearchResultIndex = rank, AppName = name, IsGame = isGame };
    }

    private static SearchPageViewModel CreateViewModel(string query)
    {
        var viewModel = new SearchPageViewModel(null!);
        var normalize = typeof(SearchPageViewModel).GetMethod("NormalizeSearchText", BindingFlags.Static | BindingFlags.NonPublic)!;
        typeof(SearchPageViewModel).GetField("normalizedSearchText", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(viewModel, normalize.Invoke(null, new object[] { query }));
        return viewModel;
    }

    private static List<AppModel> Sort(string query, List<AppModel> apps)
    {
        var viewModel = CreateViewModel(query);
        viewModel.AppsList = apps;
        return viewModel.AppsList;
    }

    private static void AssertOrder(List<AppModel> apps, params int[] expected) =>
        Assert.Equal(expected, apps.Select(app => app.Index));

    private static void AssertDisplayNumbers(List<AppModel> apps) =>
        Assert.Equal(Enumerable.Range(1, apps.Count), apps.Select(app => app.DisplayIndex));
}
