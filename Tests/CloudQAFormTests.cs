using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support.Extensions;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using System.Drawing;

namespace CloudQA.AutomationTests
{
    [TestFixture]
    public class CloudQAFormTests
    {
        private IWebDriver driver;
        private CloudQAFormPage formPage;
        private const string BASE_URL = "https://app.cloudqa.io/home/AutomationPracticeForm";
        private const int TIMEOUT_SECONDS = 30;

        [SetUp]
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArguments("--disable-blink-features=AutomationControlled");
            options.AddArguments("--no-sandbox");
            options.AddArguments("--disable-dev-shm-usage");
            options.AddUserProfilePreference("profile.default_content_setting_values.notifications", 2);
            
            driver = new ChromeDriver(options);
            driver.Manage().Window.Size = new Size(1920, 1080);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            
            formPage = new CloudQAFormPage(driver);
            
            TestLogger.Log("Test setup completed successfully");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    TakeScreenshot($"Failed_{TestContext.CurrentContext.Test.Name.ReplaceInvalidFileNameChars("_")}");
                }
            }
            finally
            {
                driver?.Quit();
                driver?.Dispose(); 
                TestLogger.Log("Test teardown completed");
            }
        }

        [Test]
        [TestCase("saurabh", "India", "Dance", TestName = "ValidFormSubmission_Scenario1")]
        [TestCase("scarlet", "Canada", "Reading", TestName = "ValidFormSubmission_Scenario2")]
        [TestCase("modi", "India", "Cricket", TestName = "ValidFormSubmission_Scenario3")]
        public void TestThreeFormFields_WithRobustLocators(string firstName, string country, string hobby)
        {
            try
            {
                TestLogger.Log($"Starting test with data: {firstName}, {country}, {hobby}");
                
                formPage.NavigateToForm(BASE_URL);
                
                TestLogger.Log("Testing First Name field...");
                formPage.EnterFirstName(firstName);
                var enteredFirstName = formPage.GetFirstNameValue();
                
                Assert.That(enteredFirstName, Is.EqualTo(firstName), "First Name was not entered correctly");
                TestLogger.Log($"✓ First Name field test passed: {enteredFirstName}");
                
                TestLogger.Log("Testing State dropdown...");
                formPage.SelectState(country);
                var selectedState = formPage.GetSelectedState();
                
                Assert.That(selectedState, Is.EqualTo(country), "State was not selected correctly");
                TestLogger.Log($"✓ State dropdown test passed: {selectedState}");
                
                TestLogger.Log("Testing Hobby checkbox...");
                formPage.SelectHobby(hobby);
                var isHobbySelected = formPage.IsHobbySelected(hobby);
                
                Assert.That(isHobbySelected, Is.True, $"Hobby '{hobby}' was not selected correctly");
                TestLogger.Log($"✓ Hobby checkbox test passed: {hobby} is selected");
                
                ValidateFieldPersistence(firstName, country, hobby);
                
                TestLogger.Log("All three form fields tested successfully!");
            }
            catch (Exception ex)
            {
                TestLogger.Log($"Test failed with error: {ex.Message}");
                
                TakeScreenshot($"Error_{TestContext.CurrentContext.Test.Name.ReplaceInvalidFileNameChars("_")}");
                throw;
            }
        }

        private void ValidateFieldPersistence(string firstName, string country, string hobby)
        {
            TestLogger.Log("Validating field persistence...");
            
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
            System.Threading.Thread.Sleep(1000);
            ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollTo(0, 0);");
            
            Assert.That(formPage.GetFirstNameValue(), Is.EqualTo(firstName), "First Name value was lost");
            Assert.That(formPage.GetSelectedState(), Is.EqualTo(country), "State selection was lost");
            Assert.That(formPage.IsHobbySelected(hobby), Is.True, $"Hobby '{hobby}' selection was lost");
            
            TestLogger.Log("✓ Field persistence validation passed");
        }

        private void TakeScreenshot(string testName)
        {
            try
            {
                var screenshot = driver.TakeScreenshot();
                
                string screenshotsDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Screenshots");
                Directory.CreateDirectory(screenshotsDir); 
                var filename = $"{testName.ReplaceInvalidFileNameChars("_")}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var filepath = Path.Combine(screenshotsDir, filename);
                screenshot.SaveAsFile(filepath);
                
                TestLogger.Log($"Screenshot saved: {filepath}");
                TestContext.AddTestAttachment(filepath, $"Screenshot for {testName}");
            }
            catch (Exception ex)
            {
                TestLogger.Log($"Failed to take screenshot: {ex.Message}");
            }
        }
    }

    internal static class StringExtensions
    {
        public static string ReplaceInvalidFileNameChars(this string filename, string replacement = "_")
        {
            return string.Join(replacement, filename.Split(Path.GetInvalidFileNameChars()))
                         .Replace(Path.DirectorySeparatorChar.ToString(), replacement)
                         .Replace(Path.AltDirectorySeparatorChar.ToString(), replacement);
        }
    }

    public class CloudQAFormPage
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;
        private readonly RobustElementLocator elementLocator;

        public CloudQAFormPage(IWebDriver driver)
        {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            this.elementLocator = new RobustElementLocator(driver, wait);
        }

        public void NavigateToForm(string url)
        {
            driver.Navigate().GoToUrl(url);
            wait.Until(ExpectedConditions.ElementIsVisible(By.TagName("form")));
            TestLogger.Log($"Successfully navigated to: {url}");
        }

        public void EnterFirstName(string firstName)
        {
            var firstNameElement = elementLocator.FindElement(
                "First Name Field",
                By.Id("fname"),                         
                By.Name("First Name"),                    
                By.XPath("//input[@placeholder='Name']"), 
                By.CssSelector("input[class*='form-control']:first-of-type") 
            );
            
            elementLocator.SafeAction(() => {
                firstNameElement.Clear();
                firstNameElement.SendKeys(firstName);
            }, "Enter First Name");
        }

        public string GetFirstNameValue()
        {
            var firstNameElement = elementLocator.FindElement(
                "First Name Field for Reading",
                By.Id("fname"),
                By.Name("First Name"),
                By.XPath("//input[@placeholder='Name']")
            );
            
            return firstNameElement.GetAttribute("value") ?? string.Empty;
        }

        public void SelectState(string stateName)
        {
            var stateDropdown = elementLocator.FindElement(
                "State Dropdown",
                By.Id("state"),                                    
                By.Name("State"),                                 
                By.XPath("//select[contains(@class,'form-control')]"), 
                By.CssSelector("select[name='State']")             
            );

            elementLocator.SafeAction(() => {
                var selectElement = new SelectElement(stateDropdown);
                selectElement.SelectByText(stateName);
            }, $"Select State: {stateName}");
        }

        public string GetSelectedState()
        {
            var stateDropdown = elementLocator.FindElement(
                "State Dropdown for Reading",
                By.Id("state"),
                By.Name("State")
            );
            
            var selectElement = new SelectElement(stateDropdown);
            return selectElement.SelectedOption.Text;
        }

        public void SelectHobby(string hobbyName)
        {
            var hobbyCheckbox = elementLocator.FindElement(
                $"{hobbyName} Hobby Checkbox",
                By.Id(hobbyName),                                           
                By.XPath($"//input[@value='{hobbyName}']"),                
                By.XPath($"//input[@type='checkbox']/..//span[text()='{hobbyName}']/../input"), 
                By.CssSelector($"input[type='checkbox'][value='{hobbyName}']") 
            );

            elementLocator.SafeAction(() => {
                if (!hobbyCheckbox.Selected)
                {
                    hobbyCheckbox.Click();
                }
            }, $"Select Hobby: {hobbyName}");
        }

        public bool IsHobbySelected(string hobbyName)
        {
            var hobbyCheckbox = elementLocator.FindElement(
                $"{hobbyName} Hobby Checkbox for Validation",
                By.Id(hobbyName),
                By.XPath($"//input[@value='{hobbyName}']")
            );
            
            return hobbyCheckbox.Selected;
        }
    }

    public class RobustElementLocator
    {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;
        private const int MAX_RETRIES = 3;

        public RobustElementLocator(IWebDriver driver, WebDriverWait wait)
        {
            this.driver = driver;
            this.wait = wait;
        }

        public IWebElement FindElement(string elementDescription, params By[] locators)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                foreach (var locator in locators)
                {
                    try
                    {
                        TestLogger.Log($"Attempt {attempt}: Trying to find '{elementDescription}' using: {locator}");
                        
                        var element = wait.Until(ExpectedConditions.ElementToBeClickable(locator));
                        
                        if (element.Displayed && element.Enabled)
                        {
                            TestLogger.Log($"✓ Successfully found '{elementDescription}' using: {locator}");
                            return element;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        TestLogger.Log($"Failed to find '{elementDescription}' using: {locator}. Error: {ex.Message}");
                    }
                }

                if (attempt < MAX_RETRIES)
                {
                    TestLogger.Log($"Retrying in 2 seconds... (Attempt {attempt + 1}/{MAX_RETRIES})");
                    System.Threading.Thread.Sleep(2000);

                    try
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState;");
                    }
                    catch {  }
                }
            }

            throw new ElementNotFoundException(
                $"Could not find element '{elementDescription}' after {MAX_RETRIES} attempts using any of the provided locators. " +
                $"Last error: {lastException?.Message ?? "No additional error information."}");
                
        }

        public void SafeAction(Action action, string actionDescription)
        {
            Exception lastException = null;

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    TestLogger.Log($"Executing action: {actionDescription} (Attempt {attempt})");
                    action();
                    TestLogger.Log($"✓ Successfully executed: {actionDescription}");
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    TestLogger.Log($"Failed to execute '{actionDescription}': {ex.Message}");
                    
                    if (attempt < MAX_RETRIES)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            throw new ActionExecutionException(
                $"Could not execute action '{actionDescription}' after {MAX_RETRIES} attempts. " +
                $"Last error: {lastException?.Message}");
        }
    }

    public class ElementNotFoundException : Exception
    {
        public ElementNotFoundException(string message) : base(message) { }
    }

    public class ActionExecutionException : Exception
    {
        public ActionExecutionException(string message) : base(message) { }
    }

    public static class TestLogger
    {
        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] {message}";
            
            TestContext.WriteLine(logMessage);
            
            try
            {
                var logFile = Path.Combine(TestContext.CurrentContext.WorkDirectory, "test-execution.log");
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch { }
        }
    }
}