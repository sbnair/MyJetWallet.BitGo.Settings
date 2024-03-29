﻿using System;
using System.Linq;
using MyJetWallet.BitGo.Settings.NoSql;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;

// ReSharper disable UnusedMember.Global

namespace MyJetWallet.BitGo.Settings.Services
{
    public class AssetMapper : IAssetMapper
    {
        private readonly IMyNoSqlServerDataReader<BitgoAssetMapEntity> _assetMap;
        private readonly IMyNoSqlServerDataReader<BitgoCoinEntity> _bitgoCoins;

        public AssetMapper(IMyNoSqlServerDataReader<BitgoAssetMapEntity> assetMap, IMyNoSqlServerDataReader<BitgoCoinEntity> bitgoCoins)
        {
            _assetMap = assetMap;
            _bitgoCoins = bitgoCoins;
        }

        public (string, string) AssetToBitgoCoinAndWallet(string brokerId, string assetSymbol)
        {
            var map = _assetMap.Get(BitgoAssetMapEntity.GeneratePartitionKey(brokerId), BitgoAssetMapEntity.GenerateRowKey(assetSymbol));

            if (map == null)
            {
                return (string.Empty, string.Empty);
            }

            return (map.BitgoCoin, map.BitgoWalletId);
        }

        public (string, string) BitgoCoinToAsset(string coin, string walletId)
        {
            var entities = _assetMap.Get().Where(e => e.BitgoWalletId == walletId && e.BitgoCoin == coin).ToList();

            if (!entities.Any())
            {
                return (string.Empty, string.Empty);
            }

            if (entities.Count() > 1)
            {
                throw new Exception($"Cannot map BitGo wallet {walletId} coin {coin} to Asset. Table: {BitgoAssetMapEntity.TableName}. Find many assets: {JsonConvert.SerializeObject(entities)}");
            }

            var entity = entities.First();

            return (entity.BrokerId, entity.AssetSymbol);
        }

        public long ConvertAmountToBitgo(string coin, double amount)
        {
            var coinSettings = _bitgoCoins.Get(BitgoCoinEntity.GeneratePartitionKey(), BitgoCoinEntity.GenerateRowKey(coin));

            if (coinSettings == null)
            {
                throw new System.Exception($"Do not found settings for bitgo coin {coin} in nosql table {BitgoCoinEntity.TableName}");
            }

            return coinSettings.AmountToAbsoluteValue(amount);
        }

        public double ConvertAmountFromBitgo(string coin, long amount)
        {
            var coinSettings = _bitgoCoins.Get(BitgoCoinEntity.GeneratePartitionKey(), BitgoCoinEntity.GenerateRowKey(coin));

            if (coinSettings == null)
            {
                throw new System.Exception($"Do not found settings for bitgo coin {coin} in nosql table {BitgoCoinEntity.TableName}");
            }

            return coinSettings.AmountFromAbsoluteValue(amount);
        }
    }
}