<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<PostModel>" %>
<%@ Import Namespace="MvcAtomFeed.Models"%>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
  Add
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
  <h2>
    Add</h2>
  <%
    using(Html.BeginForm()) {%>
  <fieldset>
    <legend>Create a new post</legend>
    <div>
      <%=Html.LabelFor(m => m.Title) %>
      <%=Html.TextBoxFor(m => m.Title) %></div>
    <div>
      <div><%=Html.LabelFor(m => m.Content) %></div>
      <%=Html.TextAreaFor(m => m.Content,10,120,null) %></div>
      <div><input type="submit" /></div>
  </fieldset>
  <%
    }
  %>
</asp:Content>
