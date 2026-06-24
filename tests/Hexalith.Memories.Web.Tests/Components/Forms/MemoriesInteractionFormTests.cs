// <copyright file="MemoriesInteractionFormTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Web.Tests.Components.Forms;

using System;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Testing;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Web.Components.Forms;
using Hexalith.Memories.Web.Components.Interaction;
using Hexalith.Memories.Web.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

public sealed class MemoriesInteractionFormTests : FrontComposerTestBase
{
    public MemoriesInteractionFormTests() => Host.ValidateVersionAlignment();

    [Fact]
    public void Form_ValidRequest_EnablesSubmitAndReportsDispatchable()
    {
        IRenderedComponent<MemoriesInteractionForm> component = RenderForm(FormFixtures.Request());

        IElement root = component.Find("[data-testid='mem-interaction-form']");
        root.GetAttribute("data-can-dispatch").ShouldBe("true");
        root.GetAttribute("data-has-errors").ShouldBe("false");

        component.Find("[data-testid='mem-form-submit']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Form_ScopeSection_RendersBeforeFields()
    {
        IRenderedComponent<MemoriesInteractionForm> component = RenderForm(FormFixtures.Request());

        string markup = component.Markup;
        markup.IndexOf("data-testid=\"mem-form-scope\"", StringComparison.Ordinal)
            .ShouldBeLessThan(markup.IndexOf("data-testid=\"mem-form-fields\"", StringComparison.Ordinal));
    }

    [Fact]
    public void Form_MissingTenant_DisablesSubmitAndShowsFieldAssociatedMessage()
    {
        IRenderedComponent<MemoriesInteractionForm> component = RenderForm(
            FormFixtures.Request(requestedTenant: " ", fields: [FormFixtures.Tenant(" "), FormFixtures.Case()]));

        component.Find("[data-testid='mem-interaction-form']").GetAttribute("data-has-errors").ShouldBe("true");
        component.Find("[data-testid='mem-form-submit']").HasAttribute("disabled").ShouldBeTrue();

        IElement tenantField = component.FindAll("[data-testid='mem-form-field']")
            .Single(e => e.GetAttribute("data-field-key") == "tenant");
        tenantField.QuerySelector("[data-testid='mem-form-field-message']")!
            .GetAttribute("data-validation-code")
            .ShouldBe(nameof(FormValidationCode.TenantRequired));

        // The value carries an aria-describedby pointing at its own message container (field association).
        tenantField.QuerySelector("[data-testid='mem-form-field-value']")!
            .GetAttribute("aria-describedby").ShouldBe("tenant-msg");
    }

    [Fact]
    public void Form_AcknowledgementRequired_ShowsCheckboxAndDisablesSubmit()
    {
        IRenderedComponent<MemoriesInteractionForm> component = RenderForm(
            FormFixtures.Request(requestedTenant: "tenant-b", currentTenant: "tenant-a"));

        component.Find("[data-testid='mem-interaction-form']").GetAttribute("data-requires-ack").ShouldBe("true");
        component.FindAll("[data-testid='mem-form-acknowledge']").ShouldNotBeEmpty();
        component.Find("[data-testid='mem-form-submit']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Form_PreAcknowledgedDangerousChange_EnablesSubmit()
    {
        IRenderedComponent<MemoriesInteractionForm> component = RenderForm(
            FormFixtures.Request(requestedTenant: "tenant-b", currentTenant: "tenant-a", acknowledged: true));

        component.Find("[data-testid='mem-interaction-form']").GetAttribute("data-can-dispatch").ShouldBe("true");
        component.Find("[data-testid='mem-form-submit']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Form_ValidSubmitClick_EmitsRequestIntent()
    {
        MemoriesFormRequest? captured = null;
        IRenderedComponent<MemoriesInteractionForm> component = Render<MemoriesInteractionForm>(parameters => parameters
            .Add(p => p.Request, FormFixtures.Request())
            .Add(p => p.OnSubmit, (MemoriesFormRequest r) => captured = r));

        component.Find("[data-testid='mem-form-submit']").Click();

        captured.ShouldNotBeNull();
        captured!.RequestedTenantId.ShouldBe("tenant-a");
    }

    [Fact]
    public void Form_BlockedSubmitClick_DoesNotEmit()
    {
        MemoriesFormRequest? captured = null;
        IRenderedComponent<MemoriesInteractionForm> component = Render<MemoriesInteractionForm>(parameters => parameters
            .Add(p => p.Request, FormFixtures.Request(isolation: EvidencePacketIsolationStatus.Unauthorized))
            .Add(p => p.OnSubmit, (MemoriesFormRequest r) => captured = r));

        component.Find("[data-testid='mem-form-submit']").Click();

        captured.ShouldBeNull();
    }

    [Fact]
    public void Form_SensitiveTenant_IsRedactedInScopeLine()
    {
        IRenderedComponent<MemoriesInteractionForm> component = RenderForm(FormFixtures.Request(
            requestedTenant: "tenant Bearer leaked-token",
            currentTenant: "tenant Bearer leaked-token"));

        IElement scopeLine = component.Find("[data-testid='mem-form-scope-line']");
        scopeLine.TextContent.ShouldNotContain("Bearer ");
        scopeLine.TextContent.ShouldContain("[REDACTED]");
    }

    [Fact]
    public void Localization_EveryFormKeyResolves()
    {
        IStringLocalizer<MemoriesWebResources> localizer =
            Services.GetRequiredService<IStringLocalizer<MemoriesWebResources>>();

        foreach (string key in AllFormKeys())
        {
            LocalizedString value = localizer[key];
            value.ResourceNotFound.ShouldBeFalse($"Missing localization resource for key '{key}'.");
            value.Value.ShouldNotBeNullOrWhiteSpace();
        }
    }

    private static IEnumerable<string> AllFormKeys()
    {
        yield return FormResourceKeys.PanelLabel;
        yield return FormResourceKeys.ScopeSectionLabel;
        yield return FormResourceKeys.TenantLabel;
        yield return FormResourceKeys.CaseLabel;
        yield return FormResourceKeys.TenantScope;
        yield return FormResourceKeys.ValidationSummaryLabel;
        yield return FormResourceKeys.AcknowledgeLabel;
        yield return FormResourceKeys.SubmitLabel;
        yield return FormResourceKeys.DispatchBlockedLabel;
        yield return FormResourceKeys.DispatchReadyLabel;
        yield return FormResourceKeys.NoMessages;
        yield return InteractionResourceKeys.SeverityLabel;

        foreach (FormValidationCode code in Enum.GetValues<FormValidationCode>())
        {
            yield return FormResourceKeys.Message(code);
        }

        foreach (InteractionSeverity severity in Enum.GetValues<InteractionSeverity>())
        {
            yield return InteractionResourceKeys.Severity(severity);
        }
    }

    private IRenderedComponent<MemoriesInteractionForm> RenderForm(MemoriesFormRequest request)
        => Render<MemoriesInteractionForm>(parameters => parameters.Add(p => p.Request, request));
}
