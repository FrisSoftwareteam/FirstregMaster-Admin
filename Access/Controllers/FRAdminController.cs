using Clear;
using FirstReg.Core;
using FirstReg.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirstReg.OnlineAccess.Controllers;

[Authorize]
[Route("admin")]
public class FRAdminController(ILogger<FRAdminController> logger, Service service, IApiClient apiClient, EStockApiUrl apiUrl)
	: BaseController(service, AuditLogSection.FrAdmin)
{
	public IActionResult Index()
	{
		return RedirectToAction(nameof(Shareholders));
	}

	[Route("shareholders")]
	public async Task<IActionResult> Shareholders(int regid, string global,
		string name, string addr, string cscs, string acc, string oldacc)
	{
		try
		{
			await LogAuditAction(AuditLogType.Search,
				$"{User.Identity.Name} searched with these parameters: RegCode={regid}, Global={global}, " +
				$"Name={name}, Address={addr}, CSCS={cscs}, Account={acc}, OldAccount={oldacc}");

			return View(new FRRegisterSHSumm()
			{
				RegId = regid,
				Global = global,
				Name = name,
				Address = addr,
				ClearingNo = cscs,
				AccountNo = acc,
				OldAccountNo = oldacc,
				ListUrl = Url.Action(nameof(GetShareholderLists), new { regid, global, name, addr, cscs, acc, oldacc }),
				ExportUrl = "",
				DetailsUrl = Url.Action(nameof(GetShareholderDetails)),
				DividendsUrl = Url.Action(nameof(GetShareholderDetails))
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex.ToString());
			TempData["error"] = ex.Message;
			return View(new FRRegisterSHSumm());
		}
	}

	private async Task<List<Bson.RegHolding>> FetchShareholderLists(int regid, string global,
		string name, string addr, string cscs, string acc, string oldacc, int page = 1, int pagesize = 10000)
	{
		string url = $"{apiUrl.GetShareholders}?regcode={regid}&global={global}&name={name}" +
			$"&addr={addr}&cscs={cscs}&acc={acc}&oldacc={oldacc}&page={page}&size={pagesize}";

		var unitArrays = await apiClient.GetAsync<List<string[]>>(url, "", Common.ApiKeyHeader);

		return unitArrays.Select(x => new Bson.RegHolding(x)).ToList();
	}

	[HttpGet("shareholders-list")]
	public async Task<IActionResult> GetShareholderLists(
		int regid, string global, string name, string addr, string cscs, string acc, string oldacc)
	{
		return await GetShareholderListz(regid, global, name, addr, cscs, acc, oldacc);
	}

	[HttpGet("shareholders-list/l")]
	public async Task<IActionResult> GetShareholderListz(int regid, string global,
		string name, string addr, string cscs, string acc, string oldacc)
	{
		try
		{
			await LogAuditAction(AuditLogType.Search,
				$"{User.Identity.Name} searched with these parameters: RegCode={regid}, Global={global}, " +
				$"Name={name}, Address={addr}, CSCS={cscs}, Account={acc}, OldAccount={oldacc}");

			var shs = await FetchShareholderLists(regid, global, name, addr, cscs, acc, oldacc);

			return Ok(new
			{
				data = shs.OrderBy(x => x.FullName).Select(x => new[]
				{
					string.Join("<br>", new [] { x.AccountNo.ToString(), x.ClearingNo }.Where(a => !string.IsNullOrEmpty(a))),
					$"{x.FullName}<br/><span class=\"fs-7 fw-normal\">{string.Join("<br>", new [] { x.Address, $"{x.Phone} {x.Mobile} {x.Email}".Replace("  ", "").Trim() }.Where(a => !string.IsNullOrEmpty(a)))}</span>",
					x.Units.ToString(),
					x.Register,
					x.AccountNo.ToString(),
					x.Id.ToString(),
					x.RegCode.ToString(),
					x.Register
				}).ToList()
			});
		}
		catch (Exception ex)
		{
			logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
			return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
		}
	}

	[HttpGet("shareholder/{regid?}/{accno?}")]
	public async Task<IActionResult> GetShareholderDetails(int regid, int accno)
	{
		try
		{
			var sh = await apiClient.GetAsync<RegSH>(
				$"{apiUrl.GetUnits}/{regid}/{accno}", "", Common.ApiKeyHeader);

			await LogAuditAction(AuditLogType.ViewShareholder,
				$"{User.Identity.Name} viewed the details of this account: RegCode={regid}, " +
				$"Account={accno}, Name={sh.FullName}, CHN={sh.ClearingNo}, Units={sh.TotalUnits}");

			return Ok(new RegisterHolderModel(sh));
		}
		catch (Exception ex)
		{
			logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
			return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
		}
	}

	[HttpGet("shareholder/{regid?}/{accno?}/download")]
	public async Task<IActionResult> DownloadShareholderDetails(int regid, int accno)
	{
		try
		{
			var sh = await apiClient.GetAsync<RegSH>(
				$"{apiUrl.GetUnits}/{regid}/{accno}", "", Common.ApiKeyHeader);

			await LogAuditAction(AuditLogType.DownloadShareholder,
				$"{User.Identity.Name} downloaded the details of this account: RegCode={regid}, " +
				$"Account={accno}, Name={sh.FullName}, CHN={sh.ClearingNo}, Units={sh.TotalUnits}");

			var regmodel = new RegisterHolderModel(sh);

			var stream = Tools.ExportToXml(regmodel);

			byte[] fileContent = stream.ToArray(); // simpler way of converting to array
			stream.Close();

			return File(fileContent, "application/force-download", $"shareholder-{accno}-{DateTime.Now:yyyMMddHHmmss}.xlsx");
		}
		catch (Exception ex)
		{
			logger.LogError($"Error: {Clear.Tools.GetAllExceptionMessage(ex)};");
			return StatusCode(StatusCodes.Status500InternalServerError, Clear.Tools.GetAllExceptionMessage(ex));
		}
	}

	#region profile

	[Route("profile")]
	public async Task<IActionResult> Profile() => View(new UserModel(
		await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower())));

	[Route("profile/update")]
	public async Task<IActionResult> UpdateProfile(UserModel model)
	{
		try
		{
			var user = await service.Data.Get<User>(x => x.UserName.ToLower() == User.Identity.Name.ToLower());

			await LogAuditAction(AuditLogType.ProfileUpdate,
				$"{User.Identity.Name} Updated their profile");

			user.FullName = model.FullName.Trim();
			//user.UserName = model.Email.Trim();
			//user.Email = model.Email.Trim();
			user.PhoneNumber = model.MobileNo.Trim();
			user.StockBroker.Street = model.Street.Trim();
			user.StockBroker.City = model.City.Trim();
			user.StockBroker.State = model.State.Trim();
			//user.StockBroker.Country = model.Country.Trim();
			user.StockBroker.SecondaryPhone = model.SecondaryPhone.Trim();
			user.StockBroker.Fax = model.PostCode.Trim();

			await service.Data.UpdateAsync(user);

			TempData["success"] = "Your profile was updated";

			return RedirectToAction("Profile");
		}
		catch (Exception ex)
		{
			TempData["error"] = Clear.Tools.GetAllExceptionMessage(ex);
		}
		return Redirect(Request.Headers[Tools.UrlReferrer].ToString());
	}

	#endregion
}