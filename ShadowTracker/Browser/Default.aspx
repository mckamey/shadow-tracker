<%@ Page Language="C#" MasterPageFile="~/Layouts/Layout.Master" Title="Browser" %>
<%@ Import Namespace="Shadow.Configuration" %>

<asp:Content runat="server" ContentPlaceHolderID="B">

	<h1><a href="../" id="page-title"><%= TrackerSettingsSection.GetSettings().DisplayName %></a></h1>
	<div style="float:left;">
		<jbst:Control runat="server"
			Name='<%# this.Context.Items["TreeView"] %>'
			InlineData='<%# new Shadow.Browser.Services.BrowseService().Browse(this.Request.Path) %>' />
	</div>
	<p class="footer"><%= TrackerSettingsSection.GetSettings().ServiceDescription %></p>

</asp:Content>
