﻿// <auto-generated />
using System;
using Geocaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Geocaching.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20190410102257_First2")]
    partial class First2
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.3-servicing-35854")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Geocaching.FoundGeocache", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("GeocacheId");

                    b.Property<int>("PersonId");

                    b.HasKey("ID");

                    b.HasIndex("GeocacheId");

                    b.HasIndex("PersonId");

                    b.ToTable("FoundGeocache");
                });

            modelBuilder.Entity("Geocaching.Geocache", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Content")
                        .HasColumnType("nvarchar(255)");

                    b.Property<double>("Latitude");

                    b.Property<double>("Longitude");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(255)");

                    b.Property<int?>("PersonId");

                    b.HasKey("ID");

                    b.HasIndex("PersonId");

                    b.ToTable("Geocache");
                });

            modelBuilder.Entity("Geocaching.Person", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("City")
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(50)");

                    b.Property<double>("Latitude");

                    b.Property<double>("Longitude");

                    b.Property<string>("StreetName")
                        .IsRequired()
                        .HasColumnType("nvarchar(50)");

                    b.Property<byte>("StreetNumber");

                    b.HasKey("ID");

                    b.ToTable("Person");
                });

            modelBuilder.Entity("Geocaching.FoundGeocache", b =>
                {
                    b.HasOne("Geocaching.Geocache", "Geocache")
                        .WithMany("FoundGeocaches")
                        .HasForeignKey("GeocacheId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Geocaching.Person", "Person")
                        .WithMany("FoundGeocaches")
                        .HasForeignKey("PersonId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Geocaching.Geocache", b =>
                {
                    b.HasOne("Geocaching.Person", "Person")
                        .WithMany()
                        .HasForeignKey("PersonId");
                });
#pragma warning restore 612, 618
        }
    }
}
