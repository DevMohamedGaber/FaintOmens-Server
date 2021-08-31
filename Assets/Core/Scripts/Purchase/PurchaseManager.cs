using UnityEngine;
//using UnityEngine.Purchasing;
//using UnityEngine.Purchasing.Security;
using System.Collections.Generic;
using Game.Purchase;
namespace Game.Components
{
    public class PurchaseManager : MonoBehaviour/*, IStoreListener*/ {
        public static PurchaseManager singleton;
        /*IStoreController controller;
        IExtensionProvider extensions;
        //ConfigurationBuilder builder;
        CrossPlatformValidator validator;
        public List<Package> products;*/

        void Start() {
            /*var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            if(products.Count > 0) {
                for(int i = 0; i < products.Count; i++)
                    builder.AddProduct(products[i].name, products[i].type);
            }
            UnityPurchasing.Initialize(this, builder);*/
            //validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
        }
        public bool Validate(Player player, string receipt) {
            /*var result = validator.Validate(receipt);
            foreach(IPurchaseReceipt receiptData in result) {
                GooglePlayReceipt google = receiptData as GooglePlayReceipt;
                if(google != null) {
                    Debug.Log(google.transactionID);
                    return GivePackageToPlayer(player, google.transactionID);
                }
                AppleInAppPurchaseReceipt apple = receiptData as AppleInAppPurchaseReceipt;
                if(apple != null) {
                    Debug.Log(apple.originalTransactionIdentifier);
                    return GivePackageToPlayer(player, apple.originalTransactionIdentifier);
                }
            }*/
            return false;
        }
        public void AddNewProduct(ProductInfo newProduct) {
            //products.Add(newProduct);
            //builder.AddProduct(newProduct.name, newProduct.type);
        }
        bool GivePackageToPlayer(Player player, string id) {
            /*for(int i = 0; i < products.Count; i++) {
                if(products[i].name == id) {
                    Mail mail = new Mail();
                    mail.currency.diamonds += products[i].diamonds;
                    mail.currency.b_diamonds += products[i].b_diamonds;
                    mail.currency.gold += products[i].gold;
                    if(products[i].items.Length > 0) {
                        mail.DefineItems(products[i].items);
                    }
                    return true;
                }
            }*/
            return false;
        }
        /*public void OnInitialized (IStoreController controller, IExtensionProvider extensions) {
            this.controller = controller;
            this.extensions = extensions;
            validator = new CrossPlatformValidator(null, null, Application.identifier);
            singleton = this;
        }
        public void OnInitializeFailed (InitializationFailureReason error) { Debug.Log("Purchase Manager Initialize Failed"); }
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e) { return PurchaseProcessingResult.Complete; }
        public void OnPurchaseFailed(Product i, PurchaseFailureReason p) {}*/
    }
     public struct ProductInfo {
        public string name;
        //public ProductType type;
        public float price;
        public float discount;
        public uint diamonds;
        public uint b_diamonds;
        public uint gold;
        public ItemSlot[] items;
    }
}