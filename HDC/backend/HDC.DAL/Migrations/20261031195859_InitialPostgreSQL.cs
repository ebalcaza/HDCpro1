using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HDC.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ALCANCE",
                columns: table => new
                {
                    ID_ALCANCE = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NOMBRE = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ALCANCE", x => x.ID_ALCANCE);
                });

            migrationBuilder.CreateTable(
                name: "PERMISOS",
                columns: table => new
                {
                    ID_PERMISO = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NOMBRE = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERMISOS", x => x.ID_PERMISO);
                });

            migrationBuilder.CreateTable(
                name: "PROVINCIA",
                columns: table => new
                {
                    ID_PROVINCIA = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRE = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROVINCIA", x => x.ID_PROVINCIA);
                });

            migrationBuilder.CreateTable(
                name: "ROL",
                columns: table => new
                {
                    ID_ROL = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NOMBRE = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ES_SISTEMA = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL", x => x.ID_ROL);
                });

            migrationBuilder.CreateTable(
                name: "UNIDAD_MEDIDA",
                columns: table => new
                {
                    ID_UNIDAD_MEDIDA = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NOMBRE = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UNIDAD_MEDIDA", x => x.ID_UNIDAD_MEDIDA);
                });

            migrationBuilder.CreateTable(
                name: "TIPO_EMISION",
                columns: table => new
                {
                    ID_TIPO_EMISION = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_ALCANCE = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRE = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TIPO_EMISION", x => x.ID_TIPO_EMISION);
                    table.ForeignKey(
                        name: "FK_TIPO_EMISION_ALCANCE_ID_ALCANCE",
                        column: x => x.ID_ALCANCE,
                        principalTable: "ALCANCE",
                        principalColumn: "ID_ALCANCE",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DISTRITO",
                columns: table => new
                {
                    ID_DISTRITO = table.Column<long>(type: "bigint", nullable: false),
                    ID_PROVINCIA = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRE = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DISTRITO", x => x.ID_DISTRITO);
                    table.ForeignKey(
                        name: "FK_DISTRITO_PROVINCIA_ID_PROVINCIA",
                        column: x => x.ID_PROVINCIA,
                        principalTable: "PROVINCIA",
                        principalColumn: "ID_PROVINCIA",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROL_PERMISO",
                columns: table => new
                {
                    ID_ROL = table.Column<long>(type: "bigint", nullable: false),
                    ID_PERMISO = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROL_PERMISO", x => new { x.ID_ROL, x.ID_PERMISO });
                    table.ForeignKey(
                        name: "FK_ROL_PERMISO_PERMISOS_ID_PERMISO",
                        column: x => x.ID_PERMISO,
                        principalTable: "PERMISOS",
                        principalColumn: "ID_PERMISO",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ROL_PERMISO_ROL_ID_ROL",
                        column: x => x.ID_ROL,
                        principalTable: "ROL",
                        principalColumn: "ID_ROL",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USUARIO",
                columns: table => new
                {
                    ID_USUARIO = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_ROL = table.Column<long>(type: "bigint", nullable: false),
                    EMAIL = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CONTRASENA = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TELEFONO = table.Column<string>(type: "character varying(30)", unicode: false, maxLength: 30, nullable: true),
                    FECHA_ALTA = table.Column<DateTime>(type: "timestamp", nullable: false),
                    FECHA_BAJA = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USUARIO", x => x.ID_USUARIO);
                    table.ForeignKey(
                        name: "FK_USUARIO_ROL_ID_ROL",
                        column: x => x.ID_ROL,
                        principalTable: "ROL",
                        principalColumn: "ID_ROL",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FUENTE_EMISION",
                columns: table => new
                {
                    ID_FUENTE_EMISION = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_UNIDAD_MEDIDA = table.Column<long>(type: "bigint", nullable: false),
                    ID_TIPO_EMISION = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRE = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    VALOR_FACTOR_EMISION = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    FECHA_ALTA = table.Column<DateTime>(type: "timestamp", nullable: false),
                    FECHA_BAJA = table.Column<DateTime>(type: "timestamp", nullable: true),
                    FECHA_MODIFICACION = table.Column<DateTime>(type: "timestamp", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FUENTE_EMISION", x => x.ID_FUENTE_EMISION);
                    table.ForeignKey(
                        name: "FK_FUENTE_EMISION_TIPO_EMISION_ID_TIPO_EMISION",
                        column: x => x.ID_TIPO_EMISION,
                        principalTable: "TIPO_EMISION",
                        principalColumn: "ID_TIPO_EMISION",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FUENTE_EMISION_UNIDAD_MEDIDA_ID_UNIDAD_MEDIDA",
                        column: x => x.ID_UNIDAD_MEDIDA,
                        principalTable: "UNIDAD_MEDIDA",
                        principalColumn: "ID_UNIDAD_MEDIDA",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CIUDAD",
                columns: table => new
                {
                    ID_CIUDAD = table.Column<long>(type: "bigint", nullable: false),
                    ID_DISTRITO = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRE = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CIUDAD", x => x.ID_CIUDAD);
                    table.ForeignKey(
                        name: "FK_CIUDAD_DISTRITO_ID_DISTRITO",
                        column: x => x.ID_DISTRITO,
                        principalTable: "DISTRITO",
                        principalColumn: "ID_DISTRITO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INDIVIDUO",
                columns: table => new
                {
                    ID_USUARIO = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRES = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    APELLIDOS = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    DNI = table.Column<string>(type: "character varying(10)", unicode: false, maxLength: 10, nullable: false),
                    FECHA_NACIMIENTO = table.Column<DateOnly>(type: "date", nullable: true),
                    GENERO = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INDIVIDUO", x => x.ID_USUARIO);
                    table.ForeignKey(
                        name: "FK_INDIVIDUO_USUARIO_ID_USUARIO",
                        column: x => x.ID_USUARIO,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ORGANIZACION",
                columns: table => new
                {
                    ID_USUARIO = table.Column<long>(type: "bigint", nullable: false),
                    RAZON_SOCIAL = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    CUIT = table.Column<string>(type: "character varying(13)", unicode: false, maxLength: 13, nullable: false),
                    AREA = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CANTIDAD_MIEMBROS = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORGANIZACION", x => x.ID_USUARIO);
                    table.ForeignKey(
                        name: "FK_ORGANIZACION_USUARIO_ID_USUARIO",
                        column: x => x.ID_USUARIO,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REPORTE",
                columns: table => new
                {
                    ID_REPORTE = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_USUARIO = table.Column<long>(type: "bigint", nullable: false),
                    NOMBRE = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DESCRIPCION = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FILTROS = table.Column<string>(type: "text", nullable: false),
                    FECHA_CREACION = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTE", x => x.ID_REPORTE);
                    table.ForeignKey(
                        name: "FK_REPORTE_USUARIO_ID_USUARIO",
                        column: x => x.ID_USUARIO,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DOMICILIO",
                columns: table => new
                {
                    ID_DOMICILIO = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_CIUDAD = table.Column<long>(type: "bigint", nullable: false),
                    ID_USUARIO = table.Column<long>(type: "bigint", nullable: false),
                    CALLE = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NUMERO = table.Column<int>(type: "integer", nullable: false),
                    DEPARTAMENTO = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PISO = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BLOCK = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MANZANA = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CODIGO_POSTAL = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    TELEFONO = table.Column<string>(type: "character varying(30)", unicode: false, maxLength: 30, nullable: true),
                    FECHA_ALTA = table.Column<DateTime>(type: "timestamp", nullable: false),
                    FECHA_BAJA = table.Column<DateTime>(type: "timestamp", nullable: true),
                    TOTAL_KG_CO2 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOMICILIO", x => x.ID_DOMICILIO);
                    table.ForeignKey(
                        name: "FK_DOMICILIO_CIUDAD_ID_CIUDAD",
                        column: x => x.ID_CIUDAD,
                        principalTable: "CIUDAD",
                        principalColumn: "ID_CIUDAD",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOMICILIO_USUARIO_ID_USUARIO",
                        column: x => x.ID_USUARIO,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "COLABORACION",
                columns: table => new
                {
                    ID_COLABORACION = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_DOMICILIO = table.Column<long>(type: "bigint", nullable: false),
                    ID_USUARIO_COLABORADOR = table.Column<long>(type: "bigint", nullable: false),
                    PERIODO_DESDE = table.Column<DateOnly>(type: "date", nullable: false),
                    PERIODO_HASTA = table.Column<DateOnly>(type: "date", nullable: true),
                    ESTADO = table.Column<string>(type: "character varying(20)", unicode: false, maxLength: 20, nullable: false),
                    MENSAJE = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FECHA_INVITACION = table.Column<DateTime>(type: "date", nullable: false),
                    FECHA_RESPUESTA = table.Column<DateTime>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COLABORACION", x => x.ID_COLABORACION);
                    table.ForeignKey(
                        name: "FK_COLABORACION_DOMICILIO_ID_DOMICILIO",
                        column: x => x.ID_DOMICILIO,
                        principalTable: "DOMICILIO",
                        principalColumn: "ID_DOMICILIO",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_COLABORACION_USUARIO_ID_USUARIO_COLABORADOR",
                        column: x => x.ID_USUARIO_COLABORADOR,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DOMICILIO_FUENTE",
                columns: table => new
                {
                    ID_DOMICILIO = table.Column<long>(type: "bigint", nullable: false),
                    ID_FUENTE_EMISION = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DOMICILIO_FUENTE", x => new { x.ID_DOMICILIO, x.ID_FUENTE_EMISION });
                    table.ForeignKey(
                        name: "FK_DOMICILIO_FUENTE_DOMICILIO_ID_DOMICILIO",
                        column: x => x.ID_DOMICILIO,
                        principalTable: "DOMICILIO",
                        principalColumn: "ID_DOMICILIO",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DOMICILIO_FUENTE_FUENTE_EMISION_ID_FUENTE_EMISION",
                        column: x => x.ID_FUENTE_EMISION,
                        principalTable: "FUENTE_EMISION",
                        principalColumn: "ID_FUENTE_EMISION",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REGISTROS",
                columns: table => new
                {
                    ID_REGISTRO = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_DOMICILIO = table.Column<long>(type: "bigint", nullable: false),
                    ID_USUARIO_CARGA = table.Column<long>(type: "bigint", nullable: false),
                    PERIODO = table.Column<DateOnly>(type: "date", nullable: false),
                    OBSERVACIONES = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TOTAL_KG_CO2 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    FECHA_CREACION = table.Column<DateTime>(type: "date", nullable: false),
                    FECHA_ULTIMA_MODIFICACION = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REGISTROS", x => x.ID_REGISTRO);
                    table.ForeignKey(
                        name: "FK_REGISTROS_DOMICILIO_ID_DOMICILIO",
                        column: x => x.ID_DOMICILIO,
                        principalTable: "DOMICILIO",
                        principalColumn: "ID_DOMICILIO",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_REGISTROS_USUARIO_ID_USUARIO_CARGA",
                        column: x => x.ID_USUARIO_CARGA,
                        principalTable: "USUARIO",
                        principalColumn: "ID_USUARIO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "COLABORACION_TIPO_EMISION",
                columns: table => new
                {
                    ID_COLABORACION = table.Column<long>(type: "bigint", nullable: false),
                    ID_TIPO_EMISION = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COLABORACION_TIPO_EMISION", x => new { x.ID_COLABORACION, x.ID_TIPO_EMISION });
                    table.ForeignKey(
                        name: "FK_COLABORACION_TIPO_EMISION_COLABORACION_ID_COLABORACION",
                        column: x => x.ID_COLABORACION,
                        principalTable: "COLABORACION",
                        principalColumn: "ID_COLABORACION",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_COLABORACION_TIPO_EMISION_TIPO_EMISION_ID_TIPO_EMISION",
                        column: x => x.ID_TIPO_EMISION,
                        principalTable: "TIPO_EMISION",
                        principalColumn: "ID_TIPO_EMISION",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DETALLE_REGISTRO",
                columns: table => new
                {
                    ID_DETALLE = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ID_REGISTRO = table.Column<long>(type: "bigint", nullable: false),
                    ID_FUENTE_EMISION = table.Column<long>(type: "bigint", nullable: false),
                    CONSUMO = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    VALOR_FACTOR_EMISION = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    KG_CO2 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DETALLE_REGISTRO", x => x.ID_DETALLE);
                    table.ForeignKey(
                        name: "FK_DETALLE_REGISTRO_FUENTE_EMISION_ID_FUENTE_EMISION",
                        column: x => x.ID_FUENTE_EMISION,
                        principalTable: "FUENTE_EMISION",
                        principalColumn: "ID_FUENTE_EMISION",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DETALLE_REGISTRO_REGISTROS_ID_REGISTRO",
                        column: x => x.ID_REGISTRO,
                        principalTable: "REGISTROS",
                        principalColumn: "ID_REGISTRO",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CIUDAD_ID_DISTRITO",
                table: "CIUDAD",
                column: "ID_DISTRITO");

            migrationBuilder.CreateIndex(
                name: "IX_COLABORACION_ID_DOMICILIO",
                table: "COLABORACION",
                column: "ID_DOMICILIO");

            migrationBuilder.CreateIndex(
                name: "IX_COLABORACION_ID_USUARIO_COLABORADOR",
                table: "COLABORACION",
                column: "ID_USUARIO_COLABORADOR");

            migrationBuilder.CreateIndex(
                name: "IX_COLABORACION_TIPO_EMISION_ID_TIPO_EMISION",
                table: "COLABORACION_TIPO_EMISION",
                column: "ID_TIPO_EMISION");

            migrationBuilder.CreateIndex(
                name: "IX_DETALLE_REGISTRO_ID_FUENTE_EMISION",
                table: "DETALLE_REGISTRO",
                column: "ID_FUENTE_EMISION");

            migrationBuilder.CreateIndex(
                name: "IX_DETALLE_REGISTRO_ID_REGISTRO",
                table: "DETALLE_REGISTRO",
                column: "ID_REGISTRO");

            migrationBuilder.CreateIndex(
                name: "IX_DISTRITO_ID_PROVINCIA",
                table: "DISTRITO",
                column: "ID_PROVINCIA");

            migrationBuilder.CreateIndex(
                name: "IX_DOMICILIO_ID_CIUDAD",
                table: "DOMICILIO",
                column: "ID_CIUDAD");

            migrationBuilder.CreateIndex(
                name: "IX_DOMICILIO_ID_USUARIO",
                table: "DOMICILIO",
                column: "ID_USUARIO");

            migrationBuilder.CreateIndex(
                name: "IX_DOMICILIO_FUENTE_ID_FUENTE_EMISION",
                table: "DOMICILIO_FUENTE",
                column: "ID_FUENTE_EMISION");

            migrationBuilder.CreateIndex(
                name: "IX_FUENTE_EMISION_ID_TIPO_EMISION",
                table: "FUENTE_EMISION",
                column: "ID_TIPO_EMISION");

            migrationBuilder.CreateIndex(
                name: "IX_FUENTE_EMISION_ID_UNIDAD_MEDIDA",
                table: "FUENTE_EMISION",
                column: "ID_UNIDAD_MEDIDA");

            migrationBuilder.CreateIndex(
                name: "IX_REGISTROS_ID_DOMICILIO",
                table: "REGISTROS",
                column: "ID_DOMICILIO");

            migrationBuilder.CreateIndex(
                name: "IX_REGISTROS_ID_USUARIO_CARGA",
                table: "REGISTROS",
                column: "ID_USUARIO_CARGA");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTE_ID_USUARIO",
                table: "REPORTE",
                column: "ID_USUARIO");

            migrationBuilder.CreateIndex(
                name: "IX_ROL_PERMISO_ID_PERMISO",
                table: "ROL_PERMISO",
                column: "ID_PERMISO");

            migrationBuilder.CreateIndex(
                name: "IX_TIPO_EMISION_ID_ALCANCE",
                table: "TIPO_EMISION",
                column: "ID_ALCANCE");

            migrationBuilder.CreateIndex(
                name: "IX_USUARIO_ID_ROL",
                table: "USUARIO",
                column: "ID_ROL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "COLABORACION_TIPO_EMISION");

            migrationBuilder.DropTable(
                name: "DETALLE_REGISTRO");

            migrationBuilder.DropTable(
                name: "DOMICILIO_FUENTE");

            migrationBuilder.DropTable(
                name: "INDIVIDUO");

            migrationBuilder.DropTable(
                name: "ORGANIZACION");

            migrationBuilder.DropTable(
                name: "REPORTE");

            migrationBuilder.DropTable(
                name: "ROL_PERMISO");

            migrationBuilder.DropTable(
                name: "COLABORACION");

            migrationBuilder.DropTable(
                name: "REGISTROS");

            migrationBuilder.DropTable(
                name: "FUENTE_EMISION");

            migrationBuilder.DropTable(
                name: "PERMISOS");

            migrationBuilder.DropTable(
                name: "DOMICILIO");

            migrationBuilder.DropTable(
                name: "TIPO_EMISION");

            migrationBuilder.DropTable(
                name: "UNIDAD_MEDIDA");

            migrationBuilder.DropTable(
                name: "CIUDAD");

            migrationBuilder.DropTable(
                name: "USUARIO");

            migrationBuilder.DropTable(
                name: "ALCANCE");

            migrationBuilder.DropTable(
                name: "DISTRITO");

            migrationBuilder.DropTable(
                name: "ROL");

            migrationBuilder.DropTable(
                name: "PROVINCIA");
        }
    }
}
