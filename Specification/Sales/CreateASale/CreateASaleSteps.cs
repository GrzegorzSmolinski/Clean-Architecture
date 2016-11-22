﻿using System;
using System.Linq;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.Application.Sales.Commands.CreateSale;
using CleanArchitecture.Common.Dates;
using CleanArchitecture.Specification.Common;
using Moq;
using NUnit.Framework;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CleanArchitecture.Specification.Sales.CreateASale
{
    [Binding]
    public class CreateASaleSteps
    {
        private readonly AppContext _appContext;
        private CreateSaleModel _model;

        public CreateASaleSteps(AppContext appContext)
        {
            _appContext = appContext;
        }

        [Given(@"the following sale info:")]
        public void GivenTheFollowingSaleInfo(Table table)
        {
            var saleInfo = table.CreateInstance<CreateSaleInfoModel>();

            _appContext.Mocker.GetMock<IDateService>()
                .Setup(p => p.GetDate())
                .Returns(saleInfo.Date);

            var mockDatabase = _appContext.Mocker.GetMock<IDatabaseContext>();
               
            var lookup = new DatabaseLookup(mockDatabase.Object);

            _model = new CreateSaleModel
            {
                CustomerId = lookup.GetCustomerId(saleInfo.Customer),
                EmployeeId = lookup.GetEmployeeId(saleInfo.Employee),
                ProductId = lookup.GetProductIdByName(saleInfo.Product),
                Quantity = saleInfo.Quantity
            };
        }
        
        [When(@"I create a sale")]
        public void WhenICreateASale()
        {
            var command = _appContext.Container.GetInstance<CreateSaleCommand>();

            command.Execute(_model);
        }
        
        [Then(@"the following sales record should be recorded:")]
        public void ThenTheFollowingSalesRecordShouldBeRecorded(Table table)
        {
            var saleRecord = table.CreateInstance<CreateSaleRecordModel>();

            var database = _appContext.Database;

            var sale = database.Sales.Last();

            var lookup = new DatabaseLookup(database);

            var customerId = lookup.GetCustomerId(saleRecord.Customer);

            var employeeId = lookup.GetEmployeeId(saleRecord.Employee);

            var productId = lookup.GetProductIdByName(saleRecord.Product);

            Assert.That(sale.Date, 
                Is.EqualTo(saleRecord.Date));

            Assert.That(sale.Customer.Id,
                Is.EqualTo(customerId));

            Assert.That(sale.Employee.Id, 
                Is.EqualTo(employeeId));

            Assert.That(sale.Product.Id,
                Is.EqualTo(productId));

            Assert.That(sale.UnitPrice, 
                Is.EqualTo(saleRecord.UnitPrice));

            Assert.That(sale.Quantity,
                Is.EqualTo(saleRecord.Quantity));

            Assert.That(sale.TotalPrice,
                Is.EqualTo(saleRecord.TotalPrice));
        }

        [Then(@"the following sale-occurred notification should be sent to the inventory system:")]
        public void ThenTheFollowingNotificationThatASaleOccurredShouldBeSentToTheInventorySystem(Table table)
        {
            var notification = table.CreateInstance<CreateSaleOccurredNotificationModel>();

            var mockInventoryClient = _appContext.Mocker.GetMock<IInventoryClient>();

            mockInventoryClient.Verify(p => p.NotifySaleOcurred(
                    notification.ProductId, 
                    notification.Quantity),
                Times.Once);
        }
    }
}