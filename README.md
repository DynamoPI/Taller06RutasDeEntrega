# DeliveryRoutes — Taller 6: Archivos Planos y Grafos

Aplicación de consola en C# (.NET) que modela el mapa de una ciudad como un **grafo dirigido y ponderado** (lista de adyacencia), valida la alcanzabilidad de las entregas con **BFS**, calcula distancias y caminos mínimos con **Dijkstra** y diseña una ruta corta y válida para recoger y entregar 30 paquetes respetando calles de un solo sentido y el orden de recogida antes de entrega.

---

## Cómo ejecutar el programa

Desde la carpeta del proyecto (`DeliveryRoutes/`):

1. **Compilar el proyecto:**
   ```bash
   dotnet build
   ```

2. **Ejecutar con argumentos (recomendado):**
   ```bash
   # Caso principal (100 ubicaciones, 30 paquetes, inicio en D)
   dotnet run -- city.txt packages.txt D

   # Ejemplo pequeño (7 ubicaciones, 3 paquetes, inicio en A)
   dotnet run -- city_mini.txt packages_mini.txt A
   ```

3. **Ejecutar de forma interactiva (sin argumentos):**
   ```bash
   dotnet run
   ```
   El programa solicitará por consola la ruta del archivo de ciudad, la ruta del archivo de paquetes y la posición inicial.

---

## Estrategia utilizada para ordenar las paradas

### ¿Por qué no se prueban todas las combinaciones?
Con 30 paquetes existen **60 paradas** (30 recogidas y 30 entregas). Si no hubiera restricciones, habría $60!$ órdenes posibles (un número de 82 cifras). Incluso aplicando la regla obligatoria de recoger cada paquete antes de entregarlo, siguen existiendo $60! / 2^{30}$ combinaciones válidas (un número de 73 cifras). Es físicamente imposible evaluar todas esas rutas en un tiempo razonable; por eso el problema se divide en dos partes:

1. **Parte exacta (Dijkstra):** Se ejecuta el algoritmo de Dijkstra (usando `PriorityQueue`) una única vez desde la posición inicial y desde cada punto donde ocurre una tarea (máximo 61 orígenes). Esto genera una tabla de distancias mínimas reales en metros respetando las calles de un solo sentido y guarda los predecesores para reconstruir el camino calle por calle.
2. **Parte heurística (`RoutePlanner`):** Sobre esa tabla de distancias se busca una ruta muy corta en pocos segundos combinando tres técnicas:
   * **Construcción por inserción barata:** Se van agregando los paquetes uno a uno a la ruta, probando todas las posiciones válidas (donde la recogida quede antes que la entrega) y eligiendo el par de posiciones que menos metros sume al recorrido.
   * **Búsqueda local (mejora iterativa):** Una vez construida la ruta, se intenta reducir aún más su distancia en un ciclo que:
     1. Extrae un paquete completo (su recogida y su entrega) y lo reinserta en la mejor posición válida disponible.
     2. Toma cada parada individual y prueba moverla a otro punto válido sin romper la regla de recogida antes de entrega.
   * **Múltiples reinicios reproducibles:** Como el resultado depende del orden en que se insertan los paquetes inicialmente, el proceso completo se repite **80 veces** barajando el orden inicial con una semilla fija (`new Random(42)` para garantizar resultados reproducibles) y se conserva la ruta más corta encontrada.

Finalmente, `IsValid()` verifica que todos los paquetes se recojan y entreguen exactamente una vez y en el orden correcto antes de imprimir el reporte.

---

## Salida del caso principal (`city.txt`, `packages.txt`, inicio `D`)

```text
Posición inicial: D
Paquetes: 30   Ubicaciones: 100   Calles: 142 (30 de un solo sentido)

1. En D: recoger paquete 18.
2. Ir de D a AR (1110 m): recoger paquete 20.
   Camino: D -> BT -> P -> CB -> AD -> AR
3. Ir de AR a BB (150 m): recoger paquete 16.
   Camino: AR -> BB
4. Ir de BB a AJ (400 m): recoger paquetes 23 y 26.
   Camino: BB -> AJ
5. Ir de AJ a BE (1660 m): recoger paquete 7.
   Camino: AJ -> BB -> AR -> AD -> CB -> P -> BT -> BE
6. Ir de BE a X (490 m): recoger paquete 12.
   Camino: BE -> E -> X
7. Ir de X a BU (560 m): recoger paquetes 8 y 28.
   Camino: X -> B -> BU
8. Ir de BU a CN (430 m): recoger paquetes 1, 2 y 3.
   Camino: BU -> CN
9. Ir de CN a AM (780 m): recoger paquete 30.
   Camino: CN -> BU -> AM
10. Ir de AM a AX (270 m): recoger paquete 17.
   Camino: AM -> AK -> AX
11. Ir de AX a AQ (170 m): recoger paquete 9.
   Camino: AX -> AQ
12. Ir de AQ a CS (560 m): recoger paquete 19.
   Camino: AQ -> BS -> CS
13. Ir de CS a BS (310 m): recoger paquete 25.
   Camino: CS -> BS
14. Ir de BS a CR (520 m): entregar paquete 20.
   Camino: BS -> CT -> CR
15. Ir de CR a CH (680 m): recoger paquete 10.
   Camino: CR -> BO -> CJ -> CH
16. Ir de CH a CU (1050 m): recoger paquete 15.
   Camino: CH -> CJ -> CV -> BX -> CU
17. Ir de CU a CV (360 m): entregar paquete 28.
   Camino: CU -> CV
18. Ir de CV a M (700 m): entregar paquete 1.
   Camino: CV -> W -> CM -> M
19. Ir de M a AE (430 m): entregar paquete 26.
   Camino: M -> CM -> AE
20. Ir de AE a AP (370 m): recoger paquete 5.
   Camino: AE -> AP
21. Ir de AP a AB (1760 m): recoger paquete 4.
   Camino: AP -> AM -> AK -> AX -> F -> BV -> AB
22. Ir de AB a BV (470 m): entregar paquete 19.
   Camino: AB -> BV
23. Ir de BV a BW (1890 m): entregar paquete 30.
   Camino: BV -> F -> AX -> S -> K -> N -> BW
24. Ir de BW a CL (270 m): recoger paquete 14.
   Camino: BW -> CL
25. Ir de CL a R (360 m): recoger paquetes 6 y 21.
   Camino: CL -> R
26. Ir de R a BN (810 m): entregar paquete 7.
   Camino: R -> CL -> BN
27. Ir de BN a AI (1130 m): recoger paquete 24.
   Camino: BN -> Y -> AV -> CG -> AI
28. Ir de AI a AO (170 m): entregar paquete 3 y recoger paquete 11.
   Camino: AI -> AO
29. Ir de AO a BM (720 m): entregar paquetes 2 y 10.
   Camino: AO -> AI -> AS -> BM
30. Ir de BM a BI (1750 m): recoger paquete 13.
   Camino: BM -> AS -> BH -> BL -> CO -> BI
31. Ir de BI a BY (990 m): entregar paquete 16.
   Camino: BI -> CO -> AZ -> V -> O -> BY
32. Ir de BY a Q (390 m): recoger paquetes 22 y 29.
   Camino: BY -> Q
33. Ir de Q a AA (1450 m): entregar paquete 12.
   Camino: Q -> BJ -> CI -> Z -> AA
34. Ir de AA a AC (900 m): entregar paquete 17.
   Camino: AA -> Z -> CI -> AC
35. Ir de AC a CD (130 m): entregar paquete 25.
   Camino: AC -> CD
36. Ir de CD a BF (710 m): entregar paquetes 18 y 23.
   Camino: CD -> BA -> AG -> BF
37. Ir de BF a AP (760 m): entregar paquetes 11 y 22.
   Camino: BF -> X -> B -> AP
38. Ir de AP a AT (350 m): entregar paquetes 14 y 15 y recoger paquete 27.
   Camino: AP -> AT
39. Ir de AT a AQ (200 m): entregar paquete 6.
   Camino: AT -> AQ
40. Ir de AQ a CT (350 m): entregar paquete 4.
   Camino: AQ -> BS -> CT
41. Ir de CT a T (1420 m): entregar paquetes 24 y 27.
   Camino: CT -> BK -> CV -> W -> CF -> T
42. Ir de T a BE (440 m): entregar paquete 21.
   Camino: T -> BE
43. Ir de BE a CP (460 m): entregar paquete 9.
   Camino: BE -> BT -> CP
44. Ir de CP a AD (670 m): entregar paquetes 5 y 13.
   Camino: CP -> BT -> P -> CB -> AD
45. Ir de AD a AR (280 m): entregar paquete 8.
   Camino: AD -> AR
46. Ir de AR a AJ (550 m): entregar paquete 29.
   Camino: AR -> BB -> AJ

Todos los paquetes fueron recogidos y entregados.
Recorrido total: 30.38 km
```

---

## Salida del ejemplo pequeño (`city\_mini.txt`, `packages\_mini.txt`, inicio `A`)

```text
Posición inicial: A
Paquetes: 3   Ubicaciones: 7   Calles: 7 (1 de un solo sentido)

1. Ir de A a E (550 m): recoger paquete 2.
   Camino: A -> B -> E
2. Ir de E a C (350 m): recoger paquete 1.
   Camino: E -> B -> C
3. Ir de C a D (200 m): entregar paquete 2.
   Camino: C -> D
4. Ir de D a G (520 m): entregar paquete 1 y recoger paquete 3.
   Camino: D -> F -> G
5. Ir de G a A (820 m): entregar paquete 3.
   Camino: G -> F -> E -> B -> A

Todos los paquetes fueron recogidos y entregados.
Recorrido total: 2.44 km
```
