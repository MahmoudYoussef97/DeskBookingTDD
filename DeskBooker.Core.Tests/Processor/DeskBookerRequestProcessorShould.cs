using DeskBooker.Core.DataInterface;
using DeskBooker.Core.Domain;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DeskBooker.Core.Processor
{
    public class DeskBookerRequestProcessorShould
    {
        private readonly DeskBookingRequestProcessor _processor;
        private readonly List<Desk> _availableDesks;
        private readonly DeskBookRequest _request;
        private readonly Mock<IDeskBookingRepository> _deskBookRepository;
        private readonly Mock<IDeskRepository> _deskRepository;
        public DeskBookerRequestProcessorShould()
        {
            _request = new DeskBookRequest
            {
                FirstName = "Ahmed",
                LastName = "Seif",
                Email = "Ahmedseif@gmail.com",
                Date = new DateTime(2020, 1, 28)
            };

            _availableDesks = new List<Desk> { new Desk { Id = 7 } };
            
            _deskBookRepository = new Mock<IDeskBookingRepository>();
            _deskRepository = new Mock<IDeskRepository>();

            _deskRepository.Setup(d => d.GetAvailbleDesks(It.IsAny<DateTime>()))
                .Returns(_availableDesks);

            _processor = new DeskBookingRequestProcessor(_deskBookRepository.Object, _deskRepository.Object);
        }
        [Fact]
        public void ReturnDeskBookingResultWithRequestValues()
        {   
            // Act
            DeskBookResult result = _processor.BookDesk(_request);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(_request.FirstName, result.FirstName);
            Assert.Equal(_request.LastName, result.LastName);
            Assert.Equal(_request.Email, result.Email);
            Assert.Equal(_request.Date, result.Date);
        }
        [Fact]
        public void ThrowExceptionIfRequestIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => _processor.BookDesk(null));
            Assert.Equal("request", exception.ParamName);
        }
        [Fact]
        public void SaveDeskBooking()
        {
            DeskBooking savedDeskBooking = null;
            _deskBookRepository.Setup(d => d.Save(It.IsAny<DeskBooking>()))
                .Callback<DeskBooking>(deskBooking => {
                    savedDeskBooking = deskBooking;
                });
            _processor.BookDesk(_request);
            _deskBookRepository.Verify(d => d.Save(It.IsAny<DeskBooking>()), Times.Once);

            Assert.NotNull(savedDeskBooking);
            Assert.Equal(_request.FirstName, savedDeskBooking.FirstName);
            Assert.Equal(_request.LastName, savedDeskBooking.LastName);
            Assert.Equal(_request.Email, savedDeskBooking.Email);
            Assert.Equal(_request.Date, savedDeskBooking.Date);
            Assert.Equal(_availableDesks.First().Id, savedDeskBooking.DeskId);
        }
        [Fact]
        public void NotSaveDeskBookinIfDeskNoDeskIsAvailable()
        {
            _availableDesks.Clear();

            _processor.BookDesk(_request);

            _deskBookRepository.Verify(d => d.Save(It.IsAny<DeskBooking>()), Times.Never);
        }
        [Theory]
        [InlineData(DeskBookingResultCode.Success, true)]
        [InlineData(DeskBookingResultCode.NoDeskAvailable, false)]
        public void ReturnExpectedResultCode(DeskBookingResultCode expectedResultCode,
            bool isDeskAvailable)
        {
            if(!isDeskAvailable)
            {
                _availableDesks.Clear();
            }
            var result = _processor.BookDesk(_request);
            Assert.Equal(expectedResultCode, result.Code);
        }
        [Theory]
        [InlineData(5, true)]
        [InlineData(null, false)]
        public void ReturnExpectedDeskBookingId(int? expectedDeskBookingId,
            bool isDeskAvailable)
        {
            if (!isDeskAvailable)
            {
                _availableDesks.Clear();
            }
            else
            {
                _deskBookRepository.Setup(d => d.Save(It.IsAny<DeskBooking>()))
                    .Callback<DeskBooking>(deskBooking => {
                        deskBooking.Id = expectedDeskBookingId.Value;
                    });
            }
            var result = _processor.BookDesk(_request);
            Assert.Equal(expectedDeskBookingId, result.DeskBookingId);
        }
    }
}
