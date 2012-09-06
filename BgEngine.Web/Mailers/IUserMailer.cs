//==============================================================================
// This file is part of BgEngine.
//
// BgEngine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// BgEngine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with BgEngine. If not, see <http://www.gnu.org/licenses/>.
//==============================================================================
// Copyright (c) 2011 Yago P�rez V�zquez
// Version: 1.0
//==============================================================================

using System.Net.Mail;

using BgEngine.Domain.EntityModel;
using BgEngine.Application.DTO;
using System.Collections.Generic;

namespace BgEngine.Web.Mailers
{ 
	public interface IUserMailer
	{
				
        MailMessage PasswordReset(string token, User user);
		MailMessage Register(string token, string to, User user);
        MailMessage ConfirmSubscription(string token, string to, SubscriptionDTO subscriptionDTO);
        MailMessage GetNewsletterHtml(List<Post> newsletterPosts, string newslettername);
	}
}