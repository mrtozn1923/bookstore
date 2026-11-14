function logged(target: unknown, ctx: ClassMethodDecoratorContext) {}
class OrderService {
    @logged
    place(): void {}
}
console.log("ok");
