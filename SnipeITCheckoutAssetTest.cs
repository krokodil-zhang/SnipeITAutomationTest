using Microsoft.Playwright;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;


class SnipeITCheckououtAssetTest
{
    private const string SnipItUrl = "https://demo.snipeitapp.com/login";

    private const string usrName = "admin";

    private const string passWord = "password";

    private const string assetModel = "macbook pro 13";

    private const string AssetStatus = "Ready to Deploy";


    static async Task Main(String[] args)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await browser.NewPageAsync();


        try
        {

            // STEP 1: Login
            await LoginSnipeIT(page);

            // STEP 2: Create New
            Asset newAsset = await CreateNewAsset(page);

            // STEP 3: Check Asset create successful
            await TestCreateAsset(page);

            // STEP 4: History Tag
            await TestHistory(page, newAsset);

            // STEP 5: Check Asset Inforamtion
            await TestAssetList(page, newAsset.AssetTag);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        await browser.CloseAsync();

    }

    public class Asset
    {
        public string AssetTag { get; set; } = "DefaultAssetTag";
        public string Target { get; set; } = "DefaultTarget";
    }

    private static async Task LoginSnipeIT(IPage page)
    {
        // open the link
        await page.GotoAsync(SnipItUrl);

        // enter usr and password
        await page.FillAsync("#username", usrName);
        await page.FillAsync("#password", passWord);

        // click the login
        await page.ClickAsync("#submit");

        Console.WriteLine("Login successful!");

    }

    private static async Task<Asset> CreateNewAsset(IPage page)
    {
        // wait page and create new assert
        await page.Locator("text=Create New").ClickAsync();

        var elementLocator = page.Locator("li.open li:nth-of-type(1) > a");

        await elementLocator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible, // wait the selector 
        });

        // click Asset
        await elementLocator.ClickAsync();

        // get the asset id

        var button = page.Locator("#asset_tag");
        var value = await button.GetAttributeAsync("value");

        Assert.False(string.IsNullOrEmpty(value), "the value should not be null or empty");

        // 打印按钮的默认值
        Console.WriteLine($"Value of #asset_tag: {value}");

        // select model
        await page.ClickAsync("#select2-model_select_id-container");
        await page.Locator(".select2-search__field").FillAsync(assetModel);

        await page.WaitForSelectorAsync(".select2-results__option");

        await page.Locator(".select2-results__option").ClickAsync();


        // select status
        await page.ClickAsync("#select2-status_select_id-container");
        await page.Locator(".select2-search__field").FillAsync(AssetStatus);
        await page.WaitForSelectorAsync(".select2-results__option");

        await page.Locator(".select2-results__option").ClickAsync();

        // select random user
        await page.ClickAsync("#select2-assigned_user_select-container");

        await page.WaitForSelectorAsync(".select2-results__option");
        await page.WaitForTimeoutAsync(2000);
        // get random user and click it 
        var options = await page.QuerySelectorAllAsync(".select2-results__option");

        var random = new Random();
        int randomIndex = random.Next(0, options.Count);
        Console.WriteLine($"the total user count: {options.Count}");

        await options[randomIndex].ClickAsync();

        var selectedUser = await options[randomIndex].InnerTextAsync();
        Console.WriteLine($"the random user is: {selectedUser}");

        // save and submit

        await page.ClickAsync("#submit_button");

        return await Task.FromResult(new Asset
        {
            AssetTag = value.ToString(),
            Target = selectedUser
        });
    }

    private static async Task TestCreateAsset(IPage page)
    {
        // check asset info
        var assetCreateInfo = page.Locator("text=Click here to");
        if (await assetCreateInfo.CountAsync() > 0)
        {
            await assetCreateInfo.ClickAsync();
            return;
        }
        Assert.True(await assetCreateInfo.CountAsync() > 0, "should have create asset info, but NUll");
        
    }

    private static async Task TestHistory(IPage page, Asset newAsset)
    {
        await page.Locator("li:nth-of-type(5) span.hidden-xs").ClickAsync();
        // check the asset model "macbook pro 13"
        String assetModelName = "(" + newAsset.AssetTag + ") - Macbook Pro";
        var hasMacbookPro13 = await page.GetByText(assetModelName).IsVisibleAsync();
        Assert.True(hasMacbookPro13, "the asset model should be " + assetModelName);
        Console.WriteLine($"Contains 'macbook pro 13': {hasMacbookPro13}");

        // // check the asset create time
        // var hasReadyToDeploy = await page.GetByText("Checkout Date: Mon Aug 11,").IsVisibleAsync();
        // Console.WriteLine($"Contains 'Ready to Deploy': {hasReadyToDeploy}");

        // check the asset target

        int indexOfOpenParen = newAsset.Target.IndexOf('(');
        string beforeParen = indexOfOpenParen > 0 ? newAsset.Target.Substring(0, indexOfOpenParen).Trim() : newAsset.Target.Trim();

        int indexOfComma = beforeParen.IndexOf(',');
        string UserName;
        if (indexOfComma > 0) // 如果有逗号
        {
            // 提取逗号前后的部分，调换顺序
            string lastName = beforeParen.Substring(0, indexOfComma).Trim();
            string firstName = beforeParen.Substring(indexOfComma + 1).Trim();
            UserName = $"{firstName} {lastName}";
        }
        else
        {
            UserName = beforeParen;
        }
        
        var hasCorrectUser = await page.Locator("li").Filter(new() { HasText = UserName }).Locator("a").IsVisibleAsync();
        Assert.True(hasCorrectUser, "the userName should be " + UserName);
        Console.WriteLine($"Contains 'user': {UserName}");

    }

    private static async Task TestAssetList(IPage page, String assetTag)
    {
        // enter the asset list
        await page.Locator("section.content-header a > i").ClickAsync();

        await page.WaitForTimeoutAsync(10000);

        await page.Locator("#webui > div > div:nth-of-type(1) span").ClickAsync();

        // await page.Locator("section.content-header li:nth-of-type(2) > a").ClickAsync();

        await page.WaitForTimeoutAsync(10000);
        await Task.Delay(5000);

        var searchInput = await page.QuerySelectorAsync("div.search input");

        Assert.False(searchInput == null, "searchInput should not invisible");
        await searchInput.ClickAsync();

        // enter the asset id to search in the list
        string searchQuery = assetTag.ToString();
        await searchInput.FillAsync(assetTag);
        Console.WriteLine($"The search asset id is: {searchQuery}");

        // wait searching
        await page.WaitForTimeoutAsync(5000);

    }
}