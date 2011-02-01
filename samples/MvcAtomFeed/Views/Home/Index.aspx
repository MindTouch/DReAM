<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<IEnumerable<PostModel>>" %>
<%@ Import Namespace="MvcAtomFeed.Models"%>
<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
  Index
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
  <h2>
    Index</h2>
    <div><%=Html.ActionLink("Add new post","Add") %></div>
  <%foreach(var post in Model) {%>
  <div class="entry">
    <div class="subject">
      <label>Subject:</label><span><%=Html.ActionLink(post.Title,"Entry",new{date = post.PathDate, title = post.PathTitle}) %></span></div>
    <div class="summary">
      <label>
        Summary:</label>
      <div>
        <%=post.Summary %></div>
    </div>
  </div>
  <%
    }%>
</asp:Content>
