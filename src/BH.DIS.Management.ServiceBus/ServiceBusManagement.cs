using Azure.Messaging.ServiceBus.Administration;
using BH.DIS.Core.Logging;
using System;
using System.Threading.Tasks;

namespace BH.DIS.Management.ServiceBus;
public interface IServiceBusManagement
{
    Task CreateRule(string topicName, string subscriptionName, string ruleName);
    Task CreateEventTypeRule(string topicName, string subscriptionName, string ruleName, string eventtype);
    Task CreateCustomRule(string topicName, string subscriptionName, string ruleName, string filter, string action);
    Task CreateSubscription(string topicName, string subscriptionName);
    Task CreateForwardSubscription(string topicName, string subscriptionName, string forwardTo);
    Task CreateTopic(string topicName);
    Task DeleteRule(string topicName, string subscriptionName, string ruleName);
    Task DeleteSubscription(string topicName, string subscriptionName);
    Task DisableSubscription(string topicName, string subscriptionName);
    Task EnableSubscription(string topicName, string subscriptionName);
    Task<bool> IsSubscriptionActive(string topicName, string subscriptionName);
    Task UpdateForwardTo(string topicName, string subscriptionName, string forwardTo);
}

public class ServiceBusManagement : IServiceBusManagement
{
    private readonly ServiceBusAdministrationClient client;
    private readonly ILogger _logger;

    public ServiceBusManagement(ServiceBusAdministrationClient client, ILogger logger = null)
    {
        this.client = client;
        _logger = logger;
    }

    public async Task CreateSubscription(string topicName, string subscriptionName)
    {
        try
        {
            var subscriptionProperties = new CreateSubscriptionOptions(topicName, subscriptionName)
            {
                MaxDeliveryCount = 10,
                LockDuration = TimeSpan.FromSeconds(30),
                EnableBatchedOperations = true,
                EnableDeadLetteringOnFilterEvaluationExceptions = true,
                RequiresSession = true
            };

            _logger?.Verbose("Creating subscription...");
            await client.CreateSubscriptionAsync(subscriptionProperties);
            _logger?.Verbose("Created subscription successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, "Could not create subscription");
            throw;
        }
    }

    public async Task CreateForwardSubscription(string topicName, string subscriptionName, string forwardTo)
    {
        try
        {
            var subscriptionProperties = new CreateSubscriptionOptions(topicName, subscriptionName)
            {
                MaxDeliveryCount = 10,
                LockDuration = TimeSpan.FromSeconds(30),
                ForwardTo = forwardTo,
                EnableBatchedOperations = true,
                EnableDeadLetteringOnFilterEvaluationExceptions = true
            };

            try
            {
                var existingSub = await client.GetSubscriptionAsync(topicName, subscriptionName);
                if (existingSub.Value.RequiresSession == true) await DeleteSubscription(topicName, subscriptionName);
            }
            catch (Exception) { } //Throws error if subscription not existing

            _logger?.Verbose("Creating subscription...");
            await client.CreateSubscriptionAsync(subscriptionProperties);
            _logger?.Verbose("Created subscription successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not create subscription {e.Message}");
            throw;
        }
    }

    public async Task DeleteSubscription(string topicName, string subscriptionName)
    {
        try
        {
            _logger?.Verbose("Creating subscription...");
            var result = await client.DeleteSubscriptionAsync(topicName, subscriptionName);
            _logger?.Verbose("Created subscription successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not delete subscription {e.Message}");
            throw;
        }
    }

    public async Task CreateRule(string topicName, string subscriptionName, string ruleName)
    {
        try
        {
            var ruleOptions = new CreateRuleOptions
            {
                Filter = new SqlRuleFilter($"user.To='{subscriptionName}'")
            };

            _logger?.Verbose("Creating rule...");
            var result = await client.CreateRuleAsync(topicName, subscriptionName, ruleOptions);
            _logger?.Verbose("Created rule successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not create rule {e.Message}");
            throw;
        }
    }

    public async Task CreateEventTypeRule(string topicName, string subscriptionName, string ruleName, string eventType)
    {
        try
        {
            var ruleOptions = new CreateRuleOptions
            {
                Filter = new SqlRuleFilter($"user.EventTypeId='{eventType}'"),
                Action = new SqlRuleAction($"SET user.From ='{topicName}'; SET user.EventId = newid(); SET user.To = '{subscriptionName}';")
            };

            _logger?.Verbose("Creating rule...");
            var result = await client.CreateRuleAsync(topicName, subscriptionName, ruleOptions);
            _logger?.Verbose("Created rule successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not create rule {e.Message}");
            throw;
        }
    }

    public async Task CreateCustomRule(string topicName, string subscriptionName, string ruleName, string filter, string action)
    {
        try
        {
            var ruleOptions = new CreateRuleOptions
            {
                Filter = new SqlRuleFilter(filter),
                Name = ruleName
            };

            if (!String.IsNullOrEmpty(action))
            {
                ruleOptions.Action = new SqlRuleAction(action);
            }

            _logger?.Verbose("Creating rule...");
            var result = await client.CreateRuleAsync(topicName, subscriptionName, ruleOptions);
            _logger?.Verbose("Created rule successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not create rule {e.Message}");
            throw;
        }
    }

    public async Task CreateTopic(string topicName)
    {
        try
        {
            var topicParams = new CreateTopicOptions(topicName)
            {
                SupportOrdering = true,
                DuplicateDetectionHistoryTimeWindow = new TimeSpan(0, 10, 0),
                EnableBatchedOperations = true,
                MaxSizeInMegabytes = 5120,
            };

            _logger?.Verbose("Creating topic...");
            await client.CreateTopicAsync(topicParams);
            _logger?.Verbose("Created topic successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not create topic {e.Message}");
            throw;
        }
    }

    public async Task DeleteRule(string topicName, string subscriptionName, string ruleName)
    {
        try
        {
            _logger?.Verbose("Deleting rule...");
            var response = await client.DeleteRuleAsync(topicName, subscriptionName, ruleName);
            _logger?.Verbose("Created rule successfully.");
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not create rule {e.Message}");
            throw;
        }
    }
    public async Task DisableSubscription(string topicName, string subscriptionName)
    {
        try
        {
            var subscription = await client.GetSubscriptionAsync(topicName, subscriptionName);
            if (subscription != null)
            {
                _logger?.Verbose("Updating status for subscription...");

                subscription.Value.Status = EntityStatus.ReceiveDisabled;
                await client.UpdateSubscriptionAsync(subscription);

                _logger?.Verbose("Status updated on subscription successfully.");
            }
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not update status for subscription {e.Message}");
            throw;
        }
    }

    public async Task EnableSubscription(string topicName, string subscriptionName)
    {

        try
        {
            var subscription = await client.GetSubscriptionAsync(topicName, subscriptionName);
            if (subscription != null)
            {
                _logger?.Verbose("Updating status for subscription...");

                subscription.Value.Status = EntityStatus.Active;
                await client.UpdateSubscriptionAsync(subscription);

                _logger?.Verbose("Status updated on subscription successfully.");
            }
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not update status for subscription {e.Message}");
            throw;
        }
    }

    public async Task UpdateForwardTo(string topicName, string subscriptionName, string forwardTo)
    {

        try
        {
            var subscription = await client.GetSubscriptionAsync(topicName, subscriptionName);
            if (subscription != null)
            {
                _logger?.Verbose("Updating forward to for subscription...");

                subscription.Value.ForwardTo = forwardTo;
                await client.UpdateSubscriptionAsync(subscription);

                _logger?.Verbose("Forward to updated on subscription successfully.");
            }
        }
        catch (Exception e)
        {
            _logger?.Error(e, $"Could not update forward to for subscription {e.Message}");
            throw;
        }
    }

    public async Task<bool> IsSubscriptionActive(string topicName, string subscriptionName)
    {
        var subscription = await client.GetSubscriptionAsync(topicName, subscriptionName);
        return subscription?.Value?.Status == EntityStatus.Active;
    }
}
