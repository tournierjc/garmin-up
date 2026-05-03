using System;

namespace Networking.InternalOmt.Dto;

public record Release(string? ComputerManifestUnicodeUrl, string? ComputerManifestUrl, CouponLabel? CouponLabel, string DataProvider, string? DiscPartNumber, string? DownloadPartNumber, string? FilesToRemoveManifestUrl, Identifier Identifier, bool IsActive, bool IsDiscFree, bool IsDownloadFree, bool IsEnabled, bool IsShippingFree, string MacClientDownloadUrl, string PcClientDownloadUrl, string ProductName, string? SdCardPartNumber, string ContentsUrl, string ProductGroupCouponLabelsUrl, string ProductGroupUrl, string RegionsUrl, DateTime DateLastModified, string LastModifiedBy);
