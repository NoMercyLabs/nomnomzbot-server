// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) NoMercy Entertainment. All rights reserved.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NoMercyBot.Application.Features.Auth.Queries.GetCurrentUser;
using NoMercyBot.Application.Features.Channels.Queries.GetChannel;
using NoMercyBot.Application.Features.Commands.Commands.CreateCommand;
using NoMercyBot.Application.Features.Commands.Commands.DeleteCommand;
using NoMercyBot.Application.Features.Commands.Queries.GetCommands;
using NoMercyBot.Application.Features.Features.Queries.GetFeatures;
using NoMercyBot.Application.Pipeline;
using NoMercyBot.Application.Pipeline.Actions;
using NoMercyBot.Application.Pipeline.Conditions;
using NoMercyBot.Application.Services.Pipeline;
using NoMercyBot.Domain.Interfaces;

namespace NoMercyBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Query handlers
        services.AddScoped<GetChannelQueryHandler>();
        services.AddScoped<GetCommandsQueryHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<GetFeaturesQueryHandler>();

        // Command handlers
        services.AddScoped<CreateCommandHandler>();
        services.AddScoped<DeleteCommandHandler>();

        // Pipeline engine
        services.AddSingleton<ICommandActionRegistry, CommandActionRegistry>();
        services.AddSingleton<IConditionEvaluatorRegistry, ConditionEvaluatorRegistry>();
        services.AddScoped<IPipelineEngine, PipelineEngine>();

        // Register actions
        services.AddTransient<ICommandAction, SendMessageAction>();
        services.AddTransient<ICommandAction, DelayAction>();
        services.AddTransient<ICommandAction, SetVariableAction>();
        services.AddTransient<ICommandAction, StopAction>();
        services.AddTransient<ICommandAction, RandomResponseAction>();

        // Register conditions
        services.AddTransient<IConditionEvaluator, VariableEqualsCondition>();

        return services;
    }
}
