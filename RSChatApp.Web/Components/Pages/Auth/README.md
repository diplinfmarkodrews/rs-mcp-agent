 Authentication Components

This folder contains the refactored authentication components for the RSChatApp. The authentication logic has been properly separated from UI components and organized into reusable components and services.

## 🏗️ Architecture

### Core Service
- **`BlazorAuthenticationService`** - Main authentication service that wraps ASP.NET Core authentication and provides a clean interface for authentication state management.

### Components

#### Primary Components
- **`AuthenticationWidget`** - Primary authentication UI component that shows Login/Logout buttons and user information. This replaces the authentication logic that was previously embedded in ChatHeader.
- **`LoginModal`** - Modal dialog for Keycloak authentication with iframe-based authentication flow.

#### Support Components  
- **`UserInfoCard`** - Displays detailed user information including avatar, name, email, and roles.
- **`AuthStatusIndicator`** - Simple status indicator showing authentication state with green/red indicator.

#### Backend Components
- **`Error.razor`** - Error page displayed when authentication fails.
- **`PopupAuthSuccess.razor`** - Success page displayed after successful authentication.

## 🔧 Usage

### Service Registration
```csharp
// In Program.cs
builder.Services.AddCustomAuthenticationService();
```

### Using AuthenticationWidget
```csharp
@using RSChatApp.Web.Components.Pages.Auth

<AuthenticationWidget OnLoginRequested="@HandleLoginRequested" />
```

### Using Authentication Service in Code
```csharp
@using RSChatApp.Web.Services.Authentication
@inject IAuthenticationService AuthenticationService

@code {
    private AuthenticationInfo authInfo = new();

    protected override async Task OnInitializedAsync()
    {
        authInfo = await AuthenticationService.GetAuthenticationInfoAsync();
        AuthenticationService.AuthenticationStateChanged += OnAuthChanged;
    }
    
    private async void OnAuthChanged(object? sender, AuthenticationInfo newAuthInfo)
    {
        authInfo = newAuthInfo;
        await InvokeAsync(StateHasChanged);
    }
}
```

## 📊 Component Features

### AuthenticationWidget
- **Conditional Rendering**: Shows Login button when not authenticated, Logout button and user name when authenticated
- **Real-time Updates**: Automatically updates when authentication state changes
- **Event Handling**: Supports OnLoginRequested callback for opening login modal

### AuthenticationService
- **Clean Interface**: Provides `AuthenticationInfo` with structured user data
- **Event-driven**: Raises events when authentication state changes
- **Automatic Cleanup**: Implements IDisposable for proper resource management
- **Error Handling**: Includes proper logging and error handling

### UserInfoCard
- **Rich Display**: Shows user avatar (initials), display name, username, email
- **Role Management**: Displays user roles as styled badges
- **Responsive**: Adapts to different screen sizes

### AuthStatusIndicator  
- **Visual Status**: Green dot for authenticated, red dot for not authenticated
- **Minimal UI**: Compact indicator suitable for headers or status bars

## 🏁 Migration Benefits

### Before (ChatHeader)
- Authentication logic mixed with UI presentation
- Direct dependency on AuthenticationStateProvider
- Repeated code for user name extraction
- Manual JavaScript execution for logout
- Tight coupling between authentication and chat functionality

### After (Refactored)
- **Separation of Concerns**: Authentication logic in dedicated service
- **Reusable Components**: AuthenticationWidget can be used anywhere
- **Clean Abstractions**: `AuthenticationInfo` provides structured data
- **Proper Event Handling**: Service-based events for state changes
- **Better Testing**: Services and components can be tested independently
- **Consistent UX**: Standardized authentication behavior across the app

## 🚀 Extensibility

The new architecture makes it easy to:
- Add new authentication-aware components
- Extend user information display
- Implement role-based UI changes
- Add authentication analytics
- Support multiple authentication providers

## 🎯 Best Practices

1. **Always use `IAuthenticationService`** instead of directly accessing `AuthenticationStateProvider`
2. **Implement `IDisposable`** in components that subscribe to authentication events
3. **Use `AuthenticationInfo`** for consistent user data structure
4. **Leverage the event system** for real-time authentication state updates
5. **Compose UI** using the provided authentication components rather than recreating authentication logic
